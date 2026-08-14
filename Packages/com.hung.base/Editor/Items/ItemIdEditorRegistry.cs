using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hung.Base;
using UnityEditor;

namespace Hung.Base.Editor
{
    public sealed class ItemIdEditorConflict
    {
        internal ItemIdEditorConflict(ItemId id, ItemIdEditorOption first, ItemIdEditorOption second)
        {
            Id = id;
            First = first;
            Second = second;
        }

        public ItemId Id { get; }
        public ItemIdEditorOption First { get; }
        public ItemIdEditorOption Second { get; }
    }

    [InitializeOnLoad]
    public sealed class ItemIdEditorRegistry
    {
        private static readonly ItemIdEditorRegistry SharedRegistry =
            new ItemIdEditorRegistry(DiscoverSources);

        private readonly Func<IEnumerable<IItemIdEditorSource>> sourceFactory;
        private IReadOnlyList<ItemIdEditorOption> cachedOptions;
        private IReadOnlyList<ItemIdEditorConflict> conflicts = Array.Empty<ItemIdEditorConflict>();
        private static readonly List<IItemIdEditorSource> RegisteredSources = new();

        static ItemIdEditorRegistry()
        {
            EditorApplication.projectChanged -= InvalidateShared;
            EditorApplication.projectChanged += InvalidateShared;
            AssemblyReloadEvents.afterAssemblyReload -= InvalidateShared;
            AssemblyReloadEvents.afterAssemblyReload += InvalidateShared;
        }

        public ItemIdEditorRegistry(IEnumerable<IItemIdEditorSource> sources)
            : this(() => new[] { (IItemIdEditorSource)new BaseItemIdsEditorSource() }
                .Concat(sources ?? Array.Empty<IItemIdEditorSource>()))
        {
        }

        public ItemIdEditorRegistry(Func<IEnumerable<IItemIdEditorSource>> sourceFactory)
        {
            this.sourceFactory = sourceFactory ?? throw new ArgumentNullException(nameof(sourceFactory));
        }

        public static ItemIdEditorRegistry Shared => SharedRegistry;
        public IReadOnlyList<ItemIdEditorConflict> Conflicts => conflicts;

        public static void RegisterSource(IItemIdEditorSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            lock (RegisteredSources)
            {
                if (!RegisteredSources.Contains(source))
                    RegisteredSources.Add(source);
            }

            SharedRegistry.Invalidate();
        }

        public IReadOnlyList<ItemIdEditorOption> GetOptions()
        {
            if (cachedOptions != null) return cachedOptions;
            cachedOptions = Build(sourceFactory(), out IReadOnlyList<ItemIdEditorConflict> diagnostics);
            conflicts = diagnostics;
            return cachedOptions;
        }

        public bool IsUnknown(ItemId id)
        {
            return id.IsValid && !GetOptions().Any(option => option.Id == id);
        }

        public bool TryParseCustomId(string raw, out ItemId id)
        {
            return ItemId.TryParse(raw, out id);
        }

        public void Invalidate()
        {
            cachedOptions = null;
            conflicts = Array.Empty<ItemIdEditorConflict>();
        }

        public static IReadOnlyList<ItemIdEditorOption> Build(
            IEnumerable<IItemIdEditorSource> sources,
            out IReadOnlyList<ItemIdEditorConflict> conflicts)
        {
            var rows = new List<ItemIdEditorOption>();
            var diagnostics = new List<ItemIdEditorConflict>();
            foreach (IItemIdEditorSource source in sources ?? Array.Empty<IItemIdEditorSource>())
            {
                if (source == null) continue;
                IEnumerable<ItemIdEditorOption> options = source.GetOptions();
                if (options == null) continue;
                rows.AddRange(options.Where(option => option.Id.IsValid));
            }

            var selected = new Dictionary<ItemId, ItemIdEditorOption>();
            foreach (ItemIdEditorOption row in rows.OrderBy(option => option.Id.Value, StringComparer.Ordinal)
                                                   .ThenBy(option => option.Label, StringComparer.Ordinal)
                                                   .ThenBy(option => option.GroupPath, StringComparer.Ordinal))
            {
                if (!selected.TryGetValue(row.Id, out ItemIdEditorOption existing))
                {
                    selected.Add(row.Id, row);
                    continue;
                }

                if (!string.Equals(existing.Label, row.Label, StringComparison.Ordinal) ||
                    !string.Equals(existing.GroupPath, row.GroupPath, StringComparison.Ordinal))
                    diagnostics.Add(new ItemIdEditorConflict(row.Id, existing, row));
            }

            conflicts = diagnostics;
            return selected.Values
                .OrderBy(option => option.GroupPath, StringComparer.Ordinal)
                .ThenBy(option => option.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<IItemIdEditorSource> DiscoverSources()
        {
            var sources = new List<IItemIdEditorSource>
            {
                new BaseItemIdsEditorSource()
            };
            lock (RegisteredSources) sources.AddRange(RegisteredSources);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsTestAssembly(assembly)) continue;
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }

                foreach (Type type in types.Where(IsDefinitionType))
                {
                    if (type == typeof(BaseItemIds) || type == typeof(ItemIdEditorRegistry)) continue;
                    IItemIdEditorSource source = TryCreateSource(type);
                    if (source != null) sources.Add(source);
                    if (type.IsAbstract || !type.IsClass) continue;
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (field.FieldType != typeof(ItemId)) continue;
                        if (field.GetValue(null) is ItemId id && id.IsValid)
                            sources.Add(new SingleItemIdEditorSource(id, field.Name));
                    }
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (property.PropertyType != typeof(ItemId) || property.GetMethod == null) continue;
                        if (property.GetValue(null) is ItemId id && id.IsValid)
                            sources.Add(new SingleItemIdEditorSource(id, property.Name));
                    }
                }
            }

            return sources;
        }

        private static bool IsDefinitionType(Type type)
        {
            return type.IsClass && type.IsPublic && type.Namespace != null &&
                !type.Namespace.StartsWith("System", StringComparison.Ordinal);
        }

        private static IItemIdEditorSource TryCreateSource(Type type)
        {
            if (!typeof(IItemIdEditorSource).IsAssignableFrom(type) ||
                type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                return null;
            try { return (IItemIdEditorSource)Activator.CreateInstance(type); }
            catch { return null; }
        }

        private static bool IsTestAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void InvalidateShared()
        {
            SharedRegistry.Invalidate();
        }

        private sealed class BaseItemIdsEditorSource : IItemIdEditorSource
        {
            public IEnumerable<ItemIdEditorOption> GetOptions()
            {
                foreach (FieldInfo field in typeof(BaseItemIds).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.GetValue(null) is ItemId id && id.IsValid)
                        yield return new ItemIdEditorOption(id, field.Name, GetGroup(id));
                }
            }
        }

        private sealed class SingleItemIdEditorSource : IItemIdEditorSource
        {
            private readonly ItemId id;
            private readonly string label;

            public SingleItemIdEditorSource(ItemId id, string label)
            {
                this.id = id;
                this.label = label;
            }

            public IEnumerable<ItemIdEditorOption> GetOptions()
            {
                yield return new ItemIdEditorOption(id, label, GetGroup(id));
            }
        }

        private static string GetGroup(ItemId id)
        {
            int separator = id.Value.IndexOf('.', StringComparison.Ordinal);
            return separator > 0 ? id.Value.Substring(0, separator) : string.Empty;
        }
    }
}

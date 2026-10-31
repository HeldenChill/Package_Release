using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hung.Base;
using UnityEditor;

namespace Hung.Base.Editor
{
    [InitializeOnLoad]
    internal static class StableIdDefinitionScan
    {
        internal readonly struct Definition
        {
            public readonly Type IdType;
            public readonly object Id;
            public readonly string MemberName;

            public Definition(Type idType, object id, string memberName)
            {
                IdType = idType;
                Id = id;
                MemberName = memberName;
            }
        }

        private static readonly object syncRoot = new();
        private static List<Definition> cachedDefinitions;
        private static List<Type> cachedSourceTypes;

        static StableIdDefinitionScan()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
            AssemblyReloadEvents.afterAssemblyReload -= Invalidate;
            AssemblyReloadEvents.afterAssemblyReload += Invalidate;
        }

        internal static int Version { get; private set; }

        internal static IReadOnlyList<Definition> Definitions
        {
            get
            {
                EnsureScanned();
                return cachedDefinitions;
            }
        }

        internal static IReadOnlyList<Type> SourceTypes
        {
            get
            {
                EnsureScanned();
                return cachedSourceTypes;
            }
        }

        internal static void Invalidate()
        {
            lock (syncRoot)
            {
                cachedDefinitions = null;
                cachedSourceTypes = null;
                Version++;
            }
        }

        internal static void Collect(IEnumerable<Type> types, List<Definition> definitions, List<Type> sourceTypes)
        {
            if (types == null) return;
            foreach (Type type in types)
            {
                if (type == null) continue;
                if (!type.IsClass || !type.IsPublic || type.Namespace == null ||
                    type.Namespace.StartsWith("System", StringComparison.Ordinal))
                    continue;

                if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null && IsSourceType(type))
                {
                    if (sourceTypes != null && !sourceTypes.Contains(type))
                        sourceTypes.Add(type);
                }

                if (type.IsAbstract) continue;

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!field.FieldType.IsValueType || !typeof(IStableId).IsAssignableFrom(field.FieldType))
                        continue;

                    object val;
                    try { val = field.GetValue(null); }
                    catch { continue; }

                    if (val is IStableId stableId && stableId.IsValid)
                    {
                        definitions?.Add(new Definition(field.FieldType, val, field.Name));
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!property.PropertyType.IsValueType || !typeof(IStableId).IsAssignableFrom(property.PropertyType) || property.GetMethod == null)
                        continue;

                    object val;
                    try { val = property.GetValue(null); }
                    catch { continue; }

                    if (val is IStableId stableId && stableId.IsValid)
                    {
                        definitions?.Add(new Definition(property.PropertyType, val, property.Name));
                    }
                }
            }
        }

        private static void EnsureScanned()
        {
            lock (syncRoot)
            {
                if (cachedDefinitions != null && cachedSourceTypes != null) return;
                var defs = new List<Definition>();
                var srcs = new List<Type>();

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (IsTestAssembly(assembly)) continue;
                    Type[] types;
                    try { types = assembly.GetTypes(); }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types.Where(t => t != null).ToArray();
                    }

                    Collect(types, defs, srcs);
                }

                cachedDefinitions = defs;
                cachedSourceTypes = srcs;
            }
        }

        private static bool IsSourceType(Type type)
        {
            if (typeof(IItemIdEditorSource).IsAssignableFrom(type))
                return true;

            foreach (Type iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IStableIdEditorSource<>))
                    return true;
            }

            return false;
        }

        private static bool IsTestAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

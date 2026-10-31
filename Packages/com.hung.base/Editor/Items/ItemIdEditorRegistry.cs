using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using UnityEditor;

namespace Hung.Base.Editor
{
    /// <summary>Diagnostic record for an <see cref="ItemId"/> defined across multiple sources with conflicting labels or group paths.</summary>
    public sealed class ItemIdEditorConflict
    {
        internal ItemIdEditorConflict(ItemId id, ItemIdEditorOption first, ItemIdEditorOption second)
        {
            Id = id;
            First = first;
            Second = second;
        }

        /// <summary>Gets the conflicting item identifier.</summary>
        public ItemId Id { get; }

        /// <summary>Gets the first option definition registered for this identifier.</summary>
        public ItemIdEditorOption First { get; }

        /// <summary>Gets the conflicting second option definition.</summary>
        public ItemIdEditorOption Second { get; }
    }

    /// <summary>Editor registry managing discovered and explicitly registered <see cref="ItemId"/> options.</summary>
    [InitializeOnLoad]
    public sealed class ItemIdEditorRegistry
    {
        private static readonly ItemIdEditorRegistry SharedRegistry = new ItemIdEditorRegistry(
            new StableIdEditorRegistry<ItemId>(CreateSharedSources));

        private readonly StableIdEditorRegistry<ItemId> core;
        private IReadOnlyList<ItemIdEditorOption> cachedMappedOptions;
        private IReadOnlyList<ItemIdEditorConflict> cachedMappedConflicts;
        private IReadOnlyList<StableIdEditorOption<ItemId>> lastCoreOptions;
        private IReadOnlyList<StableIdEditorConflict<ItemId>> lastCoreConflicts;

        /// <summary>Initializes a new instance of the <see cref="ItemIdEditorRegistry"/> class with explicit sources, prepending BaseItemIds.</summary>
        /// <param name="sources">The sequence of sources to supply options.</param>
        public ItemIdEditorRegistry(IEnumerable<IItemIdEditorSource> sources)
            : this(new StableIdEditorRegistry<ItemId>(() =>
                new IStableIdEditorSource<ItemId>[] { new StableIdConstantsSource<ItemId>(typeof(BaseItemIds)) }
                    .Concat((sources ?? Array.Empty<IItemIdEditorSource>()).Where(s => s != null).Select(s => (IStableIdEditorSource<ItemId>)new LegacySourceAdapter(s)))))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ItemIdEditorRegistry"/> class with a source factory delegate, prepending BaseItemIds.</summary>
        /// <param name="sourceFactory">Factory delegate returning sources.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceFactory"/> is null.</exception>
        public ItemIdEditorRegistry(Func<IEnumerable<IItemIdEditorSource>> sourceFactory)
            : this(new StableIdEditorRegistry<ItemId>(() =>
            {
                if (sourceFactory == null) throw new ArgumentNullException(nameof(sourceFactory));
                return new IStableIdEditorSource<ItemId>[] { new StableIdConstantsSource<ItemId>(typeof(BaseItemIds)) }
                    .Concat((sourceFactory() ?? Array.Empty<IItemIdEditorSource>()).Where(s => s != null).Select(s => (IStableIdEditorSource<ItemId>)new LegacySourceAdapter(s)));
            }))
        {
            if (sourceFactory == null) throw new ArgumentNullException(nameof(sourceFactory));
        }

        internal ItemIdEditorRegistry(StableIdEditorRegistry<ItemId> core)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
        }

        /// <summary>Gets the shared default registry instance for <see cref="ItemId"/>.</summary>
        public static ItemIdEditorRegistry Shared => SharedRegistry;

        internal StableIdEditorRegistry<ItemId> Core => core;

        /// <summary>Gets the list of detected conflicts across registered item options.</summary>
        public IReadOnlyList<ItemIdEditorConflict> Conflicts
        {
            get
            {
                IReadOnlyList<StableIdEditorConflict<ItemId>> coreConflicts = core.Conflicts;
                if (cachedMappedConflicts != null && ReferenceEquals(lastCoreConflicts, coreConflicts))
                    return cachedMappedConflicts;

                var mapped = new ItemIdEditorConflict[coreConflicts.Count];
                for (int i = 0; i < coreConflicts.Count; i++)
                {
                    StableIdEditorConflict<ItemId> c = coreConflicts[i];
                    mapped[i] = new ItemIdEditorConflict(
                        c.Id,
                        new ItemIdEditorOption(c.First.Id, c.First.Label, c.First.GroupPath),
                        new ItemIdEditorOption(c.Second.Id, c.Second.Label, c.Second.GroupPath));
                }

                cachedMappedConflicts = mapped;
                lastCoreConflicts = coreConflicts;
                return cachedMappedConflicts;
            }
        }

        /// <summary>Registers an explicit legacy source provider for <see cref="ItemId"/>.</summary>
        /// <param name="source">The source provider to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static void RegisterSource(IItemIdEditorSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            StableIdEditorRegistry<ItemId>.RegisterSource(new LegacySourceAdapter(source));
        }

        internal static bool UnregisterSourceForTests(IItemIdEditorSource source)
        {
            if (source == null) return false;
            return StableIdEditorRegistry<ItemId>.UnregisterSourceForTests(new LegacySourceAdapter(source));
        }

        /// <summary>Gets all resolved options for this registry, rebuilding from sources if cache is invalid.</summary>
        /// <returns>Read-only list of item editor options.</returns>
        public IReadOnlyList<ItemIdEditorOption> GetOptions()
        {
            IReadOnlyList<StableIdEditorOption<ItemId>> coreOptions = core.GetOptions();
            if (cachedMappedOptions != null && ReferenceEquals(lastCoreOptions, coreOptions))
                return cachedMappedOptions;

            var mapped = new ItemIdEditorOption[coreOptions.Count];
            for (int i = 0; i < coreOptions.Count; i++)
            {
                StableIdEditorOption<ItemId> opt = coreOptions[i];
                mapped[i] = new ItemIdEditorOption(opt.Id, opt.Label, opt.GroupPath);
            }

            cachedMappedOptions = mapped;
            lastCoreOptions = coreOptions;
            return cachedMappedOptions;
        }

        /// <summary>Determines whether the specified item identifier is unknown (valid but not registered).</summary>
        /// <param name="id">The item identifier to check.</param>
        /// <returns><c>true</c> if valid and not registered; otherwise, <c>false</c>.</returns>
        public bool IsUnknown(ItemId id)
        {
            return core.IsUnknown(id);
        }

        /// <summary>Attempts to parse a custom item identifier string.</summary>
        /// <param name="raw">The raw string to parse.</param>
        /// <param name="id">The parsed identifier, or default if invalid.</param>
        /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
        public bool TryParseCustomId(string raw, out ItemId id)
        {
            return ItemId.TryParse(raw, out id);
        }

        /// <summary>Invalidates cached options and conflict diagnostics.</summary>
        public void Invalidate()
        {
            core.Invalidate();
            cachedMappedOptions = null;
            cachedMappedConflicts = null;
            lastCoreOptions = null;
            lastCoreConflicts = null;
        }

        /// <summary>Builds and merges options from the given sources, resolving duplicates and reporting conflicts.</summary>
        /// <param name="sources">The sources to collect options from.</param>
        /// <param name="conflicts">Diagnostic list of conflicting option definitions.</param>
        /// <returns>Ordered, merged array of item editor options.</returns>
        public static IReadOnlyList<ItemIdEditorOption> Build(
            IEnumerable<IItemIdEditorSource> sources,
            out IReadOnlyList<ItemIdEditorConflict> conflicts)
        {
            IEnumerable<IStableIdEditorSource<ItemId>> adaptedSources = (sources ?? Array.Empty<IItemIdEditorSource>())
                .Where(s => s != null)
                .Select(s => (IStableIdEditorSource<ItemId>)new LegacySourceAdapter(s));

            IReadOnlyList<StableIdEditorOption<ItemId>> coreOptions =
                StableIdEditorRegistry<ItemId>.Build(adaptedSources, out IReadOnlyList<StableIdEditorConflict<ItemId>> coreConflicts);

            var mappedOptions = new ItemIdEditorOption[coreOptions.Count];
            for (int i = 0; i < coreOptions.Count; i++)
            {
                StableIdEditorOption<ItemId> opt = coreOptions[i];
                mappedOptions[i] = new ItemIdEditorOption(opt.Id, opt.Label, opt.GroupPath);
            }

            var mappedConflicts = new ItemIdEditorConflict[coreConflicts.Count];
            for (int i = 0; i < coreConflicts.Count; i++)
            {
                StableIdEditorConflict<ItemId> c = coreConflicts[i];
                mappedConflicts[i] = new ItemIdEditorConflict(
                    c.Id,
                    new ItemIdEditorOption(c.First.Id, c.First.Label, c.First.GroupPath),
                    new ItemIdEditorOption(c.Second.Id, c.Second.Label, c.Second.GroupPath));
            }

            conflicts = mappedConflicts;
            return mappedOptions;
        }

        private static IEnumerable<IStableIdEditorSource<ItemId>> CreateSharedSources()
        {
            var sources = new List<IStableIdEditorSource<ItemId>>
            {
                new StableIdConstantsSource<ItemId>(typeof(BaseItemIds))
            };
            sources.AddRange(StableIdEditorRegistry<ItemId>.DefaultSources());

            foreach (Type sourceType in StableIdDefinitionScan.SourceTypes)
            {
                if (typeof(IItemIdEditorSource).IsAssignableFrom(sourceType))
                {
                    try
                    {
                        var instance = (IItemIdEditorSource)Activator.CreateInstance(sourceType);
                        if (instance != null)
                            sources.Add(new LegacySourceAdapter(instance));
                    }
                    catch
                    {
                        // Skip failed instantiation
                    }
                }
            }

            return sources;
        }

        private sealed class LegacySourceAdapter : IStableIdEditorSource<ItemId>, IEquatable<LegacySourceAdapter>
        {
            private readonly IItemIdEditorSource inner;

            public LegacySourceAdapter(IItemIdEditorSource inner)
            {
                this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public IEnumerable<StableIdEditorOption<ItemId>> GetOptions()
            {
                IEnumerable<ItemIdEditorOption> options = inner.GetOptions();
                if (options == null) yield break;
                foreach (ItemIdEditorOption o in options)
                {
                    yield return new StableIdEditorOption<ItemId>(o.Id, o.Label, o.GroupPath);
                }
            }

            public bool Equals(LegacySourceAdapter other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return ReferenceEquals(inner, other.inner);
            }

            public override bool Equals(object obj) => obj is LegacySourceAdapter other && Equals(other);

            public override int GetHashCode() => inner != null ? inner.GetHashCode() : 0;
        }
    }
}

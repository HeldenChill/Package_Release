using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;

namespace Hung.Base.Editor
{
    /// <summary>Generic editor registry managing discovered and explicitly registered stable identifier options.</summary>
    /// <typeparam name="TId">The identifier struct type.</typeparam>
    public sealed class StableIdEditorRegistry<TId> where TId : struct, IStableId, IEquatable<TId>
    {
        private static readonly object syncRoot = new();
        private static readonly List<IStableIdEditorSource<TId>> RegisteredSources = new();
        private static int registeredVersion;
        private static readonly StableIdEditorRegistry<TId> SharedRegistry = new(DefaultSources);

        private readonly Func<IEnumerable<IStableIdEditorSource<TId>>> sourceFactory;
        private IReadOnlyList<StableIdEditorOption<TId>> cachedOptions;
        private IReadOnlyList<StableIdEditorConflict<TId>> conflicts = Array.Empty<StableIdEditorConflict<TId>>();
        private (int scanVersion, int regVersion) lastStamp = (-1, -1);

        /// <summary>Initializes a new instance of the <see cref="StableIdEditorRegistry{TId}"/> class using a source factory delegate.</summary>
        /// <param name="sourceFactory">Delegate returning the sequence of sources for this registry.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceFactory"/> is null.</exception>
        public StableIdEditorRegistry(Func<IEnumerable<IStableIdEditorSource<TId>>> sourceFactory)
        {
            this.sourceFactory = sourceFactory ?? throw new ArgumentNullException(nameof(sourceFactory));
        }

        /// <summary>Gets the shared default registry instance for <typeparamref name="TId"/>.</summary>
        public static StableIdEditorRegistry<TId> Shared => SharedRegistry;

        /// <summary>Gets the list of detected conflicts across registered options.</summary>
        public IReadOnlyList<StableIdEditorConflict<TId>> Conflicts
        {
            get
            {
                GetOptions();
                return conflicts;
            }
        }

        /// <summary>Registers an explicit source provider for <typeparamref name="TId"/>.</summary>
        /// <param name="source">The source provider to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static void RegisterSource(IStableIdEditorSource<TId> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            lock (syncRoot)
            {
                if (!RegisteredSources.Contains(source))
                {
                    RegisteredSources.Add(source);
                    registeredVersion++;
                }
            }

            SharedRegistry.Invalidate();
        }

        internal static bool UnregisterSourceForTests(IStableIdEditorSource<TId> source)
        {
            bool removed;
            lock (syncRoot)
            {
                removed = RegisteredSources.Remove(source);
                if (removed)
                {
                    registeredVersion++;
                }
            }

            SharedRegistry.Invalidate();
            return removed;
        }

        /// <summary>Returns default sources for <typeparamref name="TId"/>: registered sources, reflected definitions, and discovered source types.</summary>
        /// <returns>Sequence of editor sources.</returns>
        public static IEnumerable<IStableIdEditorSource<TId>> DefaultSources()
        {
            var sources = new List<IStableIdEditorSource<TId>>();
            lock (syncRoot)
            {
                sources.AddRange(RegisteredSources);
            }

            sources.Add(new ScanDefinitionsSource());

            foreach (Type sourceType in StableIdDefinitionScan.SourceTypes)
            {
                if (typeof(IStableIdEditorSource<TId>).IsAssignableFrom(sourceType))
                {
                    try
                    {
                        var instance = (IStableIdEditorSource<TId>)Activator.CreateInstance(sourceType);
                        if (instance != null) sources.Add(instance);
                    }
                    catch
                    {
                        // Skip failed instantiation
                    }
                }
            }

            return sources;
        }

        /// <summary>Gets all resolved options for this registry, rebuilding from sources if cache is invalid.</summary>
        /// <returns>Read-only list of editor options.</returns>
        public IReadOnlyList<StableIdEditorOption<TId>> GetOptions()
        {
            var currentStamp = (StableIdDefinitionScan.Version, registeredVersion);
            if (cachedOptions != null && lastStamp == currentStamp)
                return cachedOptions;

            cachedOptions = Build(sourceFactory(), out IReadOnlyList<StableIdEditorConflict<TId>> diagnostics);
            conflicts = diagnostics;
            lastStamp = currentStamp;
            return cachedOptions;
        }

        /// <summary>Determines whether the specified identifier is unknown (valid but not present in registered options).</summary>
        /// <param name="id">The identifier to check.</param>
        /// <returns><c>true</c> if valid and not registered; otherwise, <c>false</c>.</returns>
        public bool IsUnknown(TId id)
        {
            return id.IsValid && !GetOptions().Any(option => option.Id.Equals(id));
        }

        /// <summary>Invalidates cached options and conflict diagnostics.</summary>
        public void Invalidate()
        {
            cachedOptions = null;
            conflicts = Array.Empty<StableIdEditorConflict<TId>>();
            lastStamp = (-1, -1);
        }

        /// <summary>Builds and merges options from the given sources, resolving duplicates and reporting conflicts.</summary>
        /// <param name="sources">The sources to collect options from.</param>
        /// <param name="conflicts">Diagnostic list of conflicting option definitions.</param>
        /// <returns>Ordered, merged array of editor options.</returns>
        public static IReadOnlyList<StableIdEditorOption<TId>> Build(
            IEnumerable<IStableIdEditorSource<TId>> sources,
            out IReadOnlyList<StableIdEditorConflict<TId>> conflicts)
        {
            var rows = new List<StableIdEditorOption<TId>>();
            var diagnostics = new List<StableIdEditorConflict<TId>>();
            foreach (IStableIdEditorSource<TId> source in sources ?? Array.Empty<IStableIdEditorSource<TId>>())
            {
                if (source == null) continue;
                IEnumerable<StableIdEditorOption<TId>> options = source.GetOptions();
                if (options == null) continue;
                rows.AddRange(options.Where(option => option.Id.IsValid));
            }

            var selected = new Dictionary<TId, StableIdEditorOption<TId>>();
            foreach (StableIdEditorOption<TId> row in rows.OrderBy(option => option.Id.Value, StringComparer.Ordinal)
                                                          .ThenBy(option => option.Label, StringComparer.Ordinal)
                                                          .ThenBy(option => option.GroupPath, StringComparer.Ordinal))
            {
                if (!selected.TryGetValue(row.Id, out StableIdEditorOption<TId> existing))
                {
                    selected.Add(row.Id, row);
                    continue;
                }

                if (!string.Equals(existing.Label, row.Label, StringComparison.Ordinal) ||
                    !string.Equals(existing.GroupPath, row.GroupPath, StringComparison.Ordinal))
                {
                    diagnostics.Add(new StableIdEditorConflict<TId>(row.Id, existing, row));
                }
            }

            conflicts = diagnostics;
            return selected.Values
                .OrderBy(option => option.GroupPath, StringComparer.Ordinal)
                .ThenBy(option => option.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private sealed class ScanDefinitionsSource : IStableIdEditorSource<TId>
        {
            public IEnumerable<StableIdEditorOption<TId>> GetOptions()
            {
                foreach (StableIdDefinitionScan.Definition def in StableIdDefinitionScan.Definitions)
                {
                    if (def.IdType == typeof(TId) && def.Id is TId id && id.IsValid)
                    {
                        yield return new StableIdEditorOption<TId>(id, def.MemberName, StableIdGrammar.GetGroup(id.Value));
                    }
                }
            }
        }
    }
}

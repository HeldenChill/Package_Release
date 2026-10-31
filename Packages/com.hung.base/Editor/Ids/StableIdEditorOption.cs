using Hung.Base;

namespace Hung.Base.Editor
{
    /// <summary>Single option entry displayed in a stable identifier dropdown.</summary>
    /// <typeparam name="TId">The identifier struct type.</typeparam>
    public readonly struct StableIdEditorOption<TId> where TId : struct, IStableId
    {
        /// <summary>Initializes a new instance of the <see cref="StableIdEditorOption{TId}"/> struct.</summary>
        /// <param name="id">The identifier value.</param>
        /// <param name="label">The human-readable label.</param>
        /// <param name="groupPath">The hierarchical group path.</param>
        public StableIdEditorOption(TId id, string label, string groupPath)
        {
            Id = id;
            Label = string.IsNullOrEmpty(label) ? id.Value : label;
            GroupPath = groupPath ?? string.Empty;
            SearchText = id.Value;
        }

        /// <summary>Gets the identifier value.</summary>
        public TId Id { get; }

        /// <summary>Gets the human-readable display label.</summary>
        public string Label { get; }

        /// <summary>Gets the hierarchical group path.</summary>
        public string GroupPath { get; }

        /// <summary>Gets the searchable text representation.</summary>
        public string SearchText { get; }
    }
}

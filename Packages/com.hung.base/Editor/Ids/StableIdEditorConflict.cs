using Hung.Base;

namespace Hung.Base.Editor
{
    /// <summary>Diagnostic record for an identifier defined across multiple sources with conflicting labels or group paths.</summary>
    /// <typeparam name="TId">The identifier struct type.</typeparam>
    public sealed class StableIdEditorConflict<TId> where TId : struct, IStableId
    {
        internal StableIdEditorConflict(TId id, StableIdEditorOption<TId> first, StableIdEditorOption<TId> second)
        {
            Id = id;
            First = first;
            Second = second;
        }

        /// <summary>Gets the conflicting identifier.</summary>
        public TId Id { get; }

        /// <summary>Gets the first option definition registered for this identifier.</summary>
        public StableIdEditorOption<TId> First { get; }

        /// <summary>Gets the conflicting second option definition.</summary>
        public StableIdEditorOption<TId> Second { get; }
    }
}

using System;

namespace Hung.Base.Persistence
{
    /// <summary>
    /// Durable evidence that a key completed at least one successful canonical commit.
    /// Stronger than "primary file exists right now" - see convergence gaps 2/3.
    /// Implementations must never store payload bytes or secret material.
    /// </summary>
    public interface ICanonicalEvidenceStore
    {
        /// <summary>Commits a receipt for <paramref name="key"/> after a successful canonical save.</summary>
        void CommitReceipt(string key, int schemaVersion, DateTime firstCommittedUtc);

        /// <summary>Returns true when a receipt for <paramref name="key"/> has been committed.</summary>
        bool HasReceipt(string key);
    }

    /// <summary>
    /// Additive query surface for canonical-existence, kept separate from
    /// <see cref="IPersistenceService"/> so that interface's signature stays untouched.
    /// </summary>
    public interface ICanonicalExistenceQuery
    {
        bool CanonicalExistenceProven(string key);
    }
}

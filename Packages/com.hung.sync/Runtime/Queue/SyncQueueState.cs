using System.Collections.Generic;

namespace Hung.Sync
{
    /// <summary>
    /// Persisted queue contents: pending operations in original client order. Order is meaningful —
    /// reconciliation reapplies still-valid optimistic operations in the order they were formulated.
    /// </summary>
    public sealed class SyncQueueState
    {
        /// <summary>Pending operations, oldest first.</summary>
        public List<SyncQueueRecord> Pending { get; set; } = new List<SyncQueueRecord>();
    }
}

using System;
using System.Collections.Generic;
using Hung.Base.Persistence;

namespace Hung.Sync
{
    /// <summary>
    /// Durable pending-operation queue backed by <see cref="IPersistenceService"/>. It invents no
    /// files and no preference keys of its own — durability is delegated entirely to the
    /// persistence package.
    /// </summary>
    /// <remarks>
    /// The persistence API is synchronous, so every durability point below is a blocking write on
    /// the calling thread. Durability points are therefore deliberately few: enqueue, attempt
    /// record, and completion.
    /// </remarks>
    public sealed class SyncQueue
    {
        private readonly IPersistenceService persistence;
        private readonly SaveDefinition<SyncQueueState> definition;
        private readonly ISyncClock clock;
        private SyncQueueState state;

        /// <summary>Creates a queue over an already-composed persistence service.</summary>
        public SyncQueue(IPersistenceService persistence, SaveDefinition<SyncQueueState> definition, ISyncClock clock)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Pending operations in original client order, oldest first.</summary>
        public IReadOnlyList<SyncQueueRecord> Pending
        {
            get
            {
                RequireLoaded();
                return state.Pending;
            }
        }

        /// <summary>
        /// Loads durable queue contents. Because the definition is
        /// <see cref="SaveFailurePolicy.FailClosed"/>, an unreadable queue throws rather than
        /// silently yielding an empty queue that would drop unconfirmed operations.
        /// </summary>
        public void Load()
        {
            LoadResult<SyncQueueState> result = persistence.Load(definition);
            if (!result.Success)
                throw new PersistenceException(result.DiagnosticCode ?? "sync-queue-load-failed");

            state = result.Value ?? new SyncQueueState();
            if (state.Pending == null)
                state.Pending = new List<SyncQueueRecord>();
        }

        /// <summary>
        /// Appends an operation and persists immediately, so a crash between the caller's local
        /// state change and this call cannot lose the intent.
        /// </summary>
        public void Enqueue(SyncOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            RequireLoaded();

            if (IndexOf(operation.OperationId) >= 0)
                throw new InvalidOperationException(
                    $"Operation id '{operation.OperationId}' is already queued. A new business intent requires a new id.");

            state.Pending.Add(SyncQueueRecord.FromOperation(operation, clock.UtcNow));
            Persist();
        }

        /// <summary>Returns the oldest pending record without removing it.</summary>
        public bool TryPeek(out SyncQueueRecord record)
        {
            RequireLoaded();
            if (state.Pending.Count == 0)
            {
                record = null;
                return false;
            }

            record = state.Pending[0];
            return true;
        }

        /// <summary>
        /// Records one delivery attempt against an existing operation, preserving its id.
        /// </summary>
        // ponytail: attempt counts persist eagerly - one synchronous write per attempt. Coalesce
        // only if a measured queue-size benchmark shows the main-thread cost matters (decision D1).
        public void RecordAttempt(string operationId, string resultCode)
        {
            RequireLoaded();
            int index = IndexOf(operationId);
            if (index < 0)
                throw new InvalidOperationException($"Unknown operation id '{operationId}'.");

            SyncQueueRecord record = state.Pending[index];
            record.AttemptCount++;
            record.LastAttemptUtc = clock.UtcNow;
            record.LastResultCode = resultCode;
            Persist();
        }

        /// <summary>Removes a terminated operation and persists the shortened queue.</summary>
        public void Complete(string operationId)
        {
            RequireLoaded();
            int index = IndexOf(operationId);
            if (index < 0)
                throw new InvalidOperationException($"Unknown operation id '{operationId}'.");

            state.Pending.RemoveAt(index);
            Persist();
        }

        private int IndexOf(string operationId)
        {
            for (int i = 0; i < state.Pending.Count; i++)
            {
                if (string.Equals(state.Pending[i].OperationId, operationId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private void Persist()
        {
            SaveResult result = persistence.Save(definition, state);
            if (!result.Success)
                throw new PersistenceException(result.DiagnosticCode ?? "sync-queue-save-failed");
        }

        private void RequireLoaded()
        {
            if (state == null)
                throw new InvalidOperationException("Sync queue is not loaded. Call Load before use.");
        }
    }
}

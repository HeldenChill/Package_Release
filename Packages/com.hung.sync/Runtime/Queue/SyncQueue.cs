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

        // Stream key -> id of its conflicted head operation. In memory only: after a restart the
        // conflicted record is dispatched once more, conflicts again, and the product receives
        // fresh canonical state, so nothing new has to be serialized.
        private readonly Dictionary<string, string> conflictedByStream = new Dictionary<string, string>(StringComparer.Ordinal);

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
            conflictedByStream.Clear();
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
            Persist(() => state.Pending.RemoveAt(state.Pending.Count - 1));
        }

        /// <summary>
        /// Returns the oldest pending record whose stream is not blocked by a conflict, without
        /// removing it. Order within a stream is preserved; a blocked stream does not hold back
        /// other streams. Returns false when nothing is dispatchable.
        /// </summary>
        public bool TryPeek(out SyncQueueRecord record)
        {
            RequireLoaded();
            foreach (SyncQueueRecord candidate in state.Pending)
            {
                if (conflictedByStream.ContainsKey(candidate.StreamKey))
                    continue;
                record = candidate;
                return true;
            }

            record = null;
            return false;
        }

        /// <summary>
        /// Marks an operation as conflicted, blocking its stream until the product calls
        /// <see cref="Rebase"/> or <see cref="Complete"/>. Called by <see cref="SyncEngine"/>
        /// on <see cref="SyncResultKind.RevisionConflict"/>. Not persisted.
        /// </summary>
        public void MarkConflicted(string operationId)
        {
            RequireLoaded();
            int index = IndexOf(operationId);
            if (index < 0)
                throw new InvalidOperationException($"Unknown operation id '{operationId}'.");

            conflictedByStream[state.Pending[index].StreamKey] = operationId;
        }

        /// <summary>Whether a conflict currently blocks dispatch on <paramref name="streamKey"/>.</summary>
        public bool IsStreamBlocked(string streamKey)
        {
            RequireLoaded();
            return streamKey != null && conflictedByStream.ContainsKey(streamKey);
        }

        /// <summary>
        /// Reconciles a conflicted operation in place: same operation id, new expected revision,
        /// and optionally a new payload. Unblocks its stream and persists. The id stays safe to
        /// reuse because the authority never applied the conflicted attempt.
        /// </summary>
        /// <param name="operationId">The conflicted operation.</param>
        /// <param name="newExpectedRevision">Revision the reconciled intent expects, usually the reported canonical revision.</param>
        /// <param name="newPayload">Replacement payload, or null to keep the current one.</param>
        public void Rebase(string operationId, long newExpectedRevision, string newPayload)
        {
            RequireLoaded();
            if (newExpectedRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(newExpectedRevision), "Expected revision cannot be negative.");
            int index = IndexOf(operationId);
            if (index < 0)
                throw new InvalidOperationException($"Unknown operation id '{operationId}'.");

            SyncQueueRecord record = state.Pending[index];
            if (!conflictedByStream.TryGetValue(record.StreamKey, out string conflicted)
                || !string.Equals(conflicted, operationId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Operation '{operationId}' is not in conflict. Rebase is only for a reported revision conflict.");

            long previousRevision = record.ExpectedRevision;
            string previousPayload = record.Payload;
            record.ExpectedRevision = newExpectedRevision;
            if (newPayload != null)
                record.Payload = newPayload;
            Persist(() =>
            {
                record.ExpectedRevision = previousRevision;
                record.Payload = previousPayload;
            });
            conflictedByStream.Remove(record.StreamKey);
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
            int previousCount = record.AttemptCount;
            DateTime previousAttempt = record.LastAttemptUtc;
            string previousCode = record.LastResultCode;
            record.AttemptCount++;
            record.LastAttemptUtc = clock.UtcNow;
            record.LastResultCode = resultCode;
            Persist(() =>
            {
                record.AttemptCount = previousCount;
                record.LastAttemptUtc = previousAttempt;
                record.LastResultCode = previousCode;
            });
        }

        /// <summary>Removes a terminated operation and persists the shortened queue.</summary>
        public void Complete(string operationId)
        {
            RequireLoaded();
            int index = IndexOf(operationId);
            if (index < 0)
                throw new InvalidOperationException($"Unknown operation id '{operationId}'.");

            SyncQueueRecord removed = state.Pending[index];
            state.Pending.RemoveAt(index);
            Persist(() => state.Pending.Insert(index, removed));
            if (conflictedByStream.TryGetValue(removed.StreamKey, out string conflicted)
                && string.Equals(conflicted, operationId, StringComparison.Ordinal))
                conflictedByStream.Remove(removed.StreamKey);
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

        // A failed write undoes the in-memory change first, so memory always equals disk and a
        // caller retrying the same operation id after the exception is not refused as a duplicate.
        private void Persist(Action undo)
        {
            SaveResult result = persistence.Save(definition, state);
            if (result.Success)
                return;
            undo();
            throw new PersistenceException(result.DiagnosticCode ?? "sync-queue-save-failed");
        }

        private void RequireLoaded()
        {
            if (state == null)
                throw new InvalidOperationException("Sync queue is not loaded. Call Load before use.");
        }
    }
}

using System;
using System.Collections.Generic;
using Hung.Base.Persistence;

namespace Hung.Sync
{
    /// <summary>
    /// Builds the persistence definition for the pending-operation queue. The queue owns its own
    /// save key, distinct from any product definition, and always fails closed.
    /// </summary>
    public static class SyncQueueDefinition
    {
        /// <summary>Save key owned by the sync package.</summary>
        public const string Key = "sync-pending";

        /// <summary>Current schema version of the queue model.</summary>
        public const int SchemaVersion = 1;

        /// <summary>
        /// Creates the queue's save definition.
        /// <see cref="SaveFailurePolicy.FailClosed"/> is mandatory: the queue can hold unconfirmed
        /// premium and IAP operations, so defaulting after corruption would silently destroy value.
        /// </summary>
        public static SaveDefinition<SyncQueueState> Create(ISaveCodec codec, ISaveProtector protector)
        {
            if (codec == null)
                throw new ArgumentNullException(nameof(codec));
            if (protector == null)
                throw new ArgumentNullException(nameof(protector));

            return new SaveDefinition<SyncQueueState>(
                key: Key,
                currentSchemaVersion: SchemaVersion,
                createDefault: () => new SyncQueueState(),
                validate: Validate,
                migrations: Array.Empty<ISaveMigration>(),
                legacyPlayerPrefsKeys: Array.Empty<string>(),
                codec: codec,
                protector: protector,
                failurePolicy: SaveFailurePolicy.FailClosed);
        }

        /// <summary>
        /// Rejects a queue whose records are structurally unusable. Duplicate operation ids are the
        /// key invariant: they would let one intent apply twice.
        /// </summary>
        private static SaveValidationResult Validate(SyncQueueState state)
        {
            if (state?.Pending == null)
                return SaveValidationResult.Invalid("sync-queue-null", "Pending list is missing.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (SyncQueueRecord record in state.Pending)
            {
                if (record == null)
                    return SaveValidationResult.Invalid("sync-queue-null-record", "Pending contains a null record.");
                if (string.IsNullOrWhiteSpace(record.OperationId))
                    return SaveValidationResult.Invalid("sync-queue-missing-id", "Pending record has no operation id.");
                if (string.IsNullOrWhiteSpace(record.StreamKey))
                    return SaveValidationResult.Invalid("sync-queue-missing-stream", "Pending record has no stream key.");
                if (record.ExpectedRevision < 0)
                    return SaveValidationResult.Invalid("sync-queue-negative-revision", "Pending record has a negative expected revision.");
                if (!seen.Add(record.OperationId))
                    return SaveValidationResult.Invalid("sync-queue-duplicate-id", $"Duplicate operation id '{record.OperationId}'.");
            }

            return SaveValidationResult.Valid();
        }
    }
}

using System;

namespace Hung.Sync
{
    /// <summary>
    /// One durable pending operation. Mutable with a public parameterless constructor because the
    /// persistence layer constrains its models to <c>new()</c> and serializes them as JSON.
    /// Deliberately carries no authentication token and no secret material.
    /// </summary>
    public sealed class SyncQueueRecord
    {
        /// <summary>Stable idempotency key, preserved verbatim across restarts and retries.</summary>
        public string OperationId { get; set; }

        /// <summary>Synchronized aggregate id.</summary>
        public string StreamKey { get; set; }

        /// <summary>Authority revision this operation expected when it was formulated.</summary>
        public long ExpectedRevision { get; set; }

        /// <summary>Stable domain discriminator.</summary>
        public string Kind { get; set; }

        /// <summary>Serialized domain command. Opaque, never logged.</summary>
        public string Payload { get; set; }

        /// <summary>Stable audit and analytics source code.</summary>
        public string Reason { get; set; }

        /// <summary>Client clock reading when the intent was formulated.</summary>
        public DateTime ClientTimestampUtc { get; set; }

        /// <summary>Whether optimistic local application was permitted.</summary>
        public SyncAuthority Authority { get; set; }

        /// <summary>Log-correlation id.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Number of delivery attempts made so far.</summary>
        public int AttemptCount { get; set; }

        /// <summary>When this record first entered the queue.</summary>
        public DateTime FirstEnqueuedUtc { get; set; }

        /// <summary>When delivery was last attempted.</summary>
        public DateTime LastAttemptUtc { get; set; }

        /// <summary>Stable code from the most recent non-terminal result, or null if never attempted.</summary>
        public string LastResultCode { get; set; }

        /// <summary>Projects an envelope into a durable record.</summary>
        public static SyncQueueRecord FromOperation(SyncOperation operation, DateTime enqueuedUtc)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            return new SyncQueueRecord
            {
                OperationId = operation.OperationId,
                StreamKey = operation.StreamKey,
                ExpectedRevision = operation.ExpectedRevision,
                Kind = operation.Kind,
                Payload = operation.Payload,
                Reason = operation.Reason,
                ClientTimestampUtc = operation.ClientTimestampUtc,
                Authority = operation.Authority,
                CorrelationId = operation.CorrelationId,
                AttemptCount = 0,
                FirstEnqueuedUtc = enqueuedUtc,
                LastAttemptUtc = enqueuedUtc,
                LastResultCode = null
            };
        }

        /// <summary>Rebuilds the envelope, preserving the original operation id so retries stay idempotent.</summary>
        public SyncOperation ToOperation() => new SyncOperation(
            OperationId,
            StreamKey,
            ExpectedRevision,
            Kind,
            Payload,
            Reason,
            ClientTimestampUtc,
            Authority,
            CorrelationId);
    }
}

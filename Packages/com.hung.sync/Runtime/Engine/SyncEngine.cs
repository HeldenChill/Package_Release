using System;

namespace Hung.Sync
{
    /// <summary>Result of dispatching one operation, surfaced to product policy for reconciliation.</summary>
    public readonly struct SyncDispatchOutcome
    {
        /// <summary>Creates a dispatch outcome.</summary>
        public SyncDispatchOutcome(SyncResultKind kind, string operationId, long canonicalRevision, string canonicalPayload, string reasonCode)
        {
            Kind = kind;
            OperationId = operationId;
            CanonicalRevision = canonicalRevision;
            CanonicalPayload = canonicalPayload;
            ReasonCode = reasonCode;
        }

        /// <summary>Outcome category.</summary>
        public SyncResultKind Kind { get; }

        /// <summary>Operation this outcome concerns, or null when the queue was empty.</summary>
        public string OperationId { get; }

        /// <summary>Canonical revision reported by the authority.</summary>
        public long CanonicalRevision { get; }

        /// <summary>Canonical serialized state, for product-side reconciliation.</summary>
        public string CanonicalPayload { get; }

        /// <summary>Stable reason code when the authority refused.</summary>
        public string ReasonCode { get; }
    }

    /// <summary>
    /// Drives pending operations through a transport and applies queue policy to each result.
    /// It decides queue mechanics only: what a conflict or rejection *means* belongs to the product.
    /// </summary>
    public sealed class SyncEngine
    {
        private readonly SyncQueue queue;
        private readonly ISyncTransport transport;
        private readonly ISyncAuthProvider auth;
        private readonly ISyncDiagnostics diagnostics;
        private readonly ISyncClock clock;

        /// <summary>Creates an engine over a loaded queue.</summary>
        public SyncEngine(SyncQueue queue, ISyncTransport transport, ISyncAuthProvider auth, ISyncDiagnostics diagnostics, ISyncClock clock)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.auth = auth ?? throw new ArgumentNullException(nameof(auth));
            this.diagnostics = diagnostics ?? NullSyncDiagnostics.Instance;
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Dispatches the oldest pending operation. Returns an outcome whose
        /// <see cref="SyncDispatchOutcome.OperationId"/> is null when nothing was pending.
        /// </summary>
        public SyncDispatchOutcome DispatchNext()
        {
            if (!queue.TryPeek(out SyncQueueRecord record))
                return new SyncDispatchOutcome(SyncResultKind.Accepted, null, 0, null, null);

            SyncOperation operation = record.ToOperation();

            if (!auth.TryGetToken(out string token))
                return Finish(operation, SyncResult.AuthRequired(operation.CorrelationId),
                    SyncResultKind.AuthenticationRequired);

            SyncTransportResponse response = transport.Send(operation, token);
            SyncResultKind kind = SyncRetryClassifier.Classify(response);
            SyncResult result = response.Outcome == SyncTransportOutcome.Delivered
                ? response.Result
                : ResultFor(kind, response.ErrorCode, operation.CorrelationId);

            return Finish(operation, result, kind);
        }

        /// <summary>
        /// Dispatches up to <paramref name="maxOperations"/> pending operations, stopping early on a
        /// condition that would make further attempts pointless: authentication required or a
        /// retryable transport failure.
        /// </summary>
        /// <returns>How many operations were dispatched.</returns>
        public int DispatchAll(int maxOperations)
        {
            if (maxOperations <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxOperations));

            int dispatched = 0;
            for (int i = 0; i < maxOperations; i++)
            {
                if (!queue.TryPeek(out _))
                    break;

                SyncDispatchOutcome outcome = DispatchNext();
                if (outcome.OperationId == null)
                    break;

                dispatched++;

                if (outcome.Kind == SyncResultKind.AuthenticationRequired
                    || outcome.Kind == SyncResultKind.RetryableTransportFailure
                    || outcome.Kind == SyncResultKind.RevisionConflict)
                    break;
            }

            return dispatched;
        }

        private SyncDispatchOutcome Finish(SyncOperation operation, SyncResult result, SyncResultKind kind)
        {
            if (kind == SyncResultKind.AuthenticationRequired)
                auth.InvalidateToken();

            if (SyncRetryClassifier.IsTerminal(kind))
                queue.Complete(operation.OperationId);
            else
                queue.RecordAttempt(operation.OperationId, result.ReasonCode ?? DiagnosticCode(kind));

            diagnostics.Report(new SyncDiagnostic(
                DiagnosticCode(kind),
                operation.OperationId,
                operation.StreamKey,
                operation.Kind,
                kind,
                result.CanonicalRevision,
                operation.CorrelationId));

            return new SyncDispatchOutcome(
                kind, operation.OperationId, result.CanonicalRevision, result.CanonicalPayload, result.ReasonCode);
        }

        private static SyncResult ResultFor(SyncResultKind kind, string errorCode, string correlationId)
        {
            switch (kind)
            {
                case SyncResultKind.AuthenticationRequired:
                    return SyncResult.AuthRequired(correlationId);
                case SyncResultKind.PermanentProtocolFailure:
                    return SyncResult.Permanent(errorCode, correlationId);
                default:
                    return SyncResult.Retryable(errorCode, correlationId);
            }
        }

        private static string DiagnosticCode(SyncResultKind kind)
        {
            switch (kind)
            {
                case SyncResultKind.Accepted: return "sync-accepted";
                case SyncResultKind.DuplicateAccepted: return "sync-duplicate";
                case SyncResultKind.RejectedBusinessRule: return "sync-rejected";
                case SyncResultKind.RevisionConflict: return "sync-conflict";
                case SyncResultKind.AuthenticationRequired: return "sync-auth-required";
                case SyncResultKind.RetryableTransportFailure: return "sync-retryable";
                case SyncResultKind.PermanentProtocolFailure: return "sync-permanent";
                default: return "sync-unknown";
            }
        }
    }
}

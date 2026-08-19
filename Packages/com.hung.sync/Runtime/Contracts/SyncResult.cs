namespace Hung.Sync
{
    /// <summary>
    /// Outcome of one operation as decided by the authority, plus the canonical facts a client
    /// needs to commit or reconcile. Carries no exception: this package reports failures as
    /// stable string codes, matching the persistence layer's diagnostic-code convention.
    /// </summary>
    public readonly struct SyncResult
    {
        /// <summary>Creates a result. Prefer the named factory methods.</summary>
        public SyncResult(SyncResultKind kind, long canonicalRevision, string canonicalPayload, string reasonCode, string correlationId)
        {
            Kind = kind;
            CanonicalRevision = canonicalRevision;
            CanonicalPayload = canonicalPayload;
            ReasonCode = reasonCode;
            CorrelationId = correlationId;
        }

        /// <summary>Outcome category driving client behavior.</summary>
        public SyncResultKind Kind { get; }

        /// <summary>Authority revision after this operation, or the current canonical revision on conflict.</summary>
        public long CanonicalRevision { get; }

        /// <summary>Authoritative serialized state, opaque to this package. Never logged.</summary>
        public string CanonicalPayload { get; }

        /// <summary>Stable machine-readable reason, surfaced to product policy and diagnostics.</summary>
        public string ReasonCode { get; }

        /// <summary>Correlation id echoed from the originating operation.</summary>
        public string CorrelationId { get; }

        /// <summary>Authority applied the operation and advanced the revision.</summary>
        public static SyncResult Accepted(long revision, string canonicalPayload, string correlationId)
            => new SyncResult(SyncResultKind.Accepted, revision, canonicalPayload, null, correlationId);

        /// <summary>Authority had already applied this operation id and returned the original result.</summary>
        public static SyncResult Duplicate(long revision, string canonicalPayload, string correlationId)
            => new SyncResult(SyncResultKind.DuplicateAccepted, revision, canonicalPayload, null, correlationId);

        /// <summary>Authority refused on a business rule.</summary>
        public static SyncResult Rejected(string reasonCode, string correlationId)
            => new SyncResult(SyncResultKind.RejectedBusinessRule, 0, null, reasonCode, correlationId);

        /// <summary>Expected revision was stale; canonical state is returned for reconciliation.</summary>
        public static SyncResult Conflict(long canonicalRevision, string canonicalPayload, string correlationId)
            => new SyncResult(SyncResultKind.RevisionConflict, canonicalRevision, canonicalPayload, null, correlationId);

        /// <summary>Authentication is required or expired; no value may be granted.</summary>
        public static SyncResult AuthRequired(string correlationId)
            => new SyncResult(SyncResultKind.AuthenticationRequired, 0, null, "auth-required", correlationId);

        /// <summary>Transient failure; the pending record survives and retries with the same id.</summary>
        public static SyncResult Retryable(string reasonCode, string correlationId)
            => new SyncResult(SyncResultKind.RetryableTransportFailure, 0, null, reasonCode, correlationId);

        /// <summary>Unrecoverable failure; automatic retry stops and evidence is preserved.</summary>
        public static SyncResult Permanent(string reasonCode, string correlationId)
            => new SyncResult(SyncResultKind.PermanentProtocolFailure, 0, null, reasonCode, correlationId);
    }
}

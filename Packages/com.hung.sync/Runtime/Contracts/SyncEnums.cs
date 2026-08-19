namespace Hung.Sync
{
    /// <summary>
    /// Whether an operation may apply locally before the authority confirms it.
    /// Set by product policy, never chosen ad hoc at a call site.
    /// </summary>
    public enum SyncAuthority
    {
        /// <summary>Low-risk operation: apply locally, persist, queue, reconcile later.</summary>
        OptimisticAllowed = 0,

        /// <summary>High-value operation: grant no value until the authority accepts it.</summary>
        ConfirmationRequired = 1
    }

    /// <summary>Outcome category for one processed operation.</summary>
    public enum SyncResultKind
    {
        /// <summary>Authority accepted the operation and returned a new canonical revision.</summary>
        Accepted = 0,

        /// <summary>Authority had already applied this operation id; original result returned, mutation not repeated.</summary>
        DuplicateAccepted = 1,

        /// <summary>Authority refused on a business rule. Roll back or compensate optimistic state.</summary>
        RejectedBusinessRule = 2,

        /// <summary>Expected revision was stale. Reconcile against canonical state; never last-write-wins.</summary>
        RevisionConflict = 3,

        /// <summary>Authentication is required or expired. Pause protected operations; grant no value.</summary>
        AuthenticationRequired = 4,

        /// <summary>Transient transport failure. Keep the pending record and retry with the same operation id.</summary>
        RetryableTransportFailure = 5,

        /// <summary>Unrecoverable protocol failure. Preserve evidence, stop automatic retry, emit diagnostics.</summary>
        PermanentProtocolFailure = 6
    }

    /// <summary>
    /// Raw transport-level outcome, before policy classification.
    /// <see cref="SyncRetryClassifier"/> maps this onto <see cref="SyncResultKind"/>.
    /// </summary>
    public enum SyncTransportOutcome
    {
        /// <summary>Transport reached the authority and carries its result.</summary>
        Delivered = 0,

        /// <summary>No network path available.</summary>
        NetworkUnavailable = 1,

        /// <summary>Request timed out before a result arrived.</summary>
        Timeout = 2,

        /// <summary>Auth token missing, rejected, or expired.</summary>
        AuthExpired = 3,

        /// <summary>Malformed or unsupported protocol response.</summary>
        ProtocolError = 4
    }
}

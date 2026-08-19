namespace Hung.Sync
{
    /// <summary>
    /// Structured, payload-free diagnostic record. This type deliberately has no payload and no
    /// token field: redaction is guaranteed by its shape rather than by caller discipline.
    /// </summary>
    public readonly struct SyncDiagnostic
    {
        /// <summary>Creates a diagnostic record.</summary>
        public SyncDiagnostic(string code, string operationId, string streamKey, string kind, SyncResultKind resultKind, long revision, string correlationId)
        {
            Code = code;
            OperationId = operationId;
            StreamKey = streamKey;
            Kind = kind;
            ResultKind = resultKind;
            Revision = revision;
            CorrelationId = correlationId;
        }

        /// <summary>Stable diagnostic code.</summary>
        public string Code { get; }

        /// <summary>Operation this diagnostic concerns.</summary>
        public string OperationId { get; }

        /// <summary>Synchronized aggregate id.</summary>
        public string StreamKey { get; }

        /// <summary>Domain discriminator, recorded verbatim and never interpreted.</summary>
        public string Kind { get; }

        /// <summary>Result category observed.</summary>
        public SyncResultKind ResultKind { get; }

        /// <summary>Revision observed at the time of this diagnostic.</summary>
        public long Revision { get; }

        /// <summary>Correlation id spanning persistence, queue, transport, and result.</summary>
        public string CorrelationId { get; }
    }
}

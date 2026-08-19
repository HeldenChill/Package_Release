using System;
using System.IO;

namespace Hung.Sync
{
    /// <summary>
    /// One synchronizable intent. Immutable, domain-opaque, and safe to persist.
    /// The generic sync layer never interprets <see cref="Kind"/> or <see cref="Payload"/>.
    /// </summary>
    public sealed class SyncOperation
    {
        /// <summary>Creates an envelope, validating every field that has a durable contract.</summary>
        /// <param name="operationId">Stable idempotency key. Reused across transport retries; a new business intent gets a new id.</param>
        /// <param name="streamKey">Synchronized aggregate id, for example <c>pvm.wallet</c>. Must satisfy the persistence key charset.</param>
        /// <param name="expectedRevision">Last authority revision known to this client. Zero means "no revision seen yet".</param>
        /// <param name="kind">Stable domain discriminator. Never interpreted here.</param>
        /// <param name="payload">Serialized domain command. Opaque. Never logged.</param>
        /// <param name="reason">Stable audit and analytics source code.</param>
        /// <param name="clientTimestampUtc">Diagnostics and plausibility input only. Never authority. Normalized to UTC.</param>
        /// <param name="authority">Whether optimistic local application is permitted.</param>
        /// <param name="correlationId">Ties local persistence, queue, transport attempt, and result together in logs.</param>
        public SyncOperation(
            string operationId,
            string streamKey,
            long expectedRevision,
            string kind,
            string payload,
            string reason,
            DateTime clientTimestampUtc,
            SyncAuthority authority,
            string correlationId)
        {
            OperationId = RequireText(operationId, nameof(operationId));
            StreamKey = ValidateStreamKey(streamKey);
            if (expectedRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRevision), "Expected revision cannot be negative.");

            ExpectedRevision = expectedRevision;
            Kind = RequireText(kind, nameof(kind));
            Payload = payload ?? string.Empty;
            Reason = RequireText(reason, nameof(reason));
            ClientTimestampUtc = clientTimestampUtc.Kind == DateTimeKind.Utc
                ? clientTimestampUtc
                : clientTimestampUtc.ToUniversalTime();
            Authority = authority;
            CorrelationId = RequireText(correlationId, nameof(correlationId));
        }

        /// <summary>Stable idempotency key, reused across every transport retry of this intent.</summary>
        public string OperationId { get; }

        /// <summary>Synchronized aggregate id.</summary>
        public string StreamKey { get; }

        /// <summary>Authority revision this operation expects to apply on top of.</summary>
        public long ExpectedRevision { get; }

        /// <summary>Stable domain discriminator, opaque to this package.</summary>
        public string Kind { get; }

        /// <summary>Serialized domain command. Opaque, never logged.</summary>
        public string Payload { get; }

        /// <summary>Stable audit and analytics source code.</summary>
        public string Reason { get; }

        /// <summary>Client clock reading, for diagnostics only. Never treated as authority.</summary>
        public DateTime ClientTimestampUtc { get; }

        /// <summary>Whether optimistic local application is permitted for this operation.</summary>
        public SyncAuthority Authority { get; }

        /// <summary>Log-correlation id spanning persistence, queue, transport, and result.</summary>
        public string CorrelationId { get; }

        /// <summary>
        /// Applies the same charset rule as the persistence layer's save-key validation, so a
        /// stream key can always be mapped onto a save key without escaping.
        /// </summary>
        public static string ValidateStreamKey(string streamKey)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key cannot be empty.", nameof(streamKey));
            if (streamKey == "." || streamKey == ".." || streamKey.Length > 120
                || Path.IsPathRooted(streamKey) || streamKey.Contains("/") || streamKey.Contains("\\"))
                throw new ArgumentException($"Invalid stream key '{streamKey}'.", nameof(streamKey));
            if (streamKey.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"Invalid stream key '{streamKey}'.", nameof(streamKey));
            return streamKey;
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
            return value;
        }
    }
}

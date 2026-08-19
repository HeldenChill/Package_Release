using System;

namespace Hung.Sync
{
    /// <summary>
    /// Transport-level response wrapping either a delivered authority result or a transport failure.
    /// </summary>
    public readonly struct SyncTransportResponse
    {
        /// <summary>Creates a transport response. Prefer the named factory methods.</summary>
        public SyncTransportResponse(SyncTransportOutcome outcome, SyncResult result, string errorCode)
        {
            Outcome = outcome;
            Result = result;
            ErrorCode = errorCode;
        }

        /// <summary>Raw transport outcome, before policy classification.</summary>
        public SyncTransportOutcome Outcome { get; }

        /// <summary>Authority result. Meaningful only when <see cref="Outcome"/> is <see cref="SyncTransportOutcome.Delivered"/>.</summary>
        public SyncResult Result { get; }

        /// <summary>Stable transport error code when delivery failed.</summary>
        public string ErrorCode { get; }

        /// <summary>The transport reached the authority and carries its result.</summary>
        public static SyncTransportResponse Delivered(SyncResult result)
            => new SyncTransportResponse(SyncTransportOutcome.Delivered, result, null);

        /// <summary>The transport failed before obtaining an authority result.</summary>
        public static SyncTransportResponse Failed(SyncTransportOutcome outcome, string errorCode)
            => new SyncTransportResponse(outcome, default, errorCode);
    }

    /// <summary>
    /// Sends one operation to an authority. Implemented outside this package by the simulator
    /// (development) or a real adapter (production). Deliberately one operation at a time;
    /// batching is out of scope for the first version.
    /// </summary>
    public interface ISyncTransport
    {
        /// <summary>Delivers one operation and returns the authority's response.</summary>
        /// <param name="operation">The envelope to deliver. Its payload stays opaque.</param>
        /// <param name="authToken">Opaque auth token, or null when the transport needs none.</param>
        SyncTransportResponse Send(SyncOperation operation, string authToken);
    }

    /// <summary>
    /// Supplies an opaque authentication token. Tokens are never persisted by the queue and
    /// never written to diagnostics.
    /// </summary>
    public interface ISyncAuthProvider
    {
        /// <summary>Returns the current token, or false when authentication is required.</summary>
        bool TryGetToken(out string token);

        /// <summary>Marks the current token unusable after the authority rejected it.</summary>
        void InvalidateToken();
    }

    /// <summary>
    /// Injectable UTC clock. Keeps this package free of Unity APIs and makes fault scripts and
    /// tests deterministic.
    /// </summary>
    public interface ISyncClock
    {
        /// <summary>Current UTC time.</summary>
        DateTime UtcNow { get; }
    }

    /// <summary>Receives payload-free structured diagnostics.</summary>
    public interface ISyncDiagnostics
    {
        /// <summary>Reports one diagnostic record.</summary>
        void Report(SyncDiagnostic diagnostic);
    }

    /// <summary>Diagnostics sink that discards everything. Used when no sink is configured.</summary>
    public sealed class NullSyncDiagnostics : ISyncDiagnostics
    {
        /// <summary>Shared instance.</summary>
        public static readonly NullSyncDiagnostics Instance = new NullSyncDiagnostics();

        private NullSyncDiagnostics() { }

        /// <inheritdoc />
        public void Report(SyncDiagnostic diagnostic) { }
    }
}

using System;
using System.Collections.Generic;

namespace Hung.Sync.Simulator
{
    /// <summary>
    /// DEVELOPMENT AND TEST ONLY. Deterministic in-memory authority: accounts, per-stream canonical
    /// state and revision, operation-id deduplication, offline and auth controls, and scripted faults.
    /// Never interprets a domain payload — accepted payloads are stored verbatim as canonical state.
    /// </summary>
    public sealed class SyncSimulatorServer
    {
        private sealed class StreamState
        {
            public long Revision;
            public string Payload = "{}";
            public readonly Dictionary<string, SyncResult> AppliedOperations =
                new Dictionary<string, SyncResult>(StringComparer.Ordinal);
        }

        private readonly Dictionary<string, Dictionary<string, StreamState>> accounts =
            new Dictionary<string, Dictionary<string, StreamState>>(StringComparer.Ordinal);

        private readonly SyncFaultScript faultScript;
        private bool offline;
        private bool authExpired;

        /// <summary>Creates a simulator, optionally driven by a fault script.</summary>
        public SyncSimulatorServer(SyncFaultScript faultScript = null)
            => this.faultScript = faultScript ?? new SyncFaultScript();

        /// <summary>Number of operations processed since construction or the last reset.</summary>
        public int AttemptCount { get; private set; }

        /// <summary>Whether the simulator is refusing connections.</summary>
        public bool IsOffline => offline;

        /// <summary>Whether the current auth token is considered expired.</summary>
        public bool IsAuthExpired => authExpired;

        /// <summary>Registers an account. Unknown accounts produce permanent failures.</summary>
        public void CreateAccount(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Account id cannot be empty.", nameof(accountId));

            if (!accounts.ContainsKey(accountId))
                accounts[accountId] = new Dictionary<string, StreamState>(StringComparer.Ordinal);
        }

        /// <summary>Simulates loss or restoration of connectivity.</summary>
        public void SetOffline(bool value) => offline = value;

        /// <summary>Simulates auth-token expiry.</summary>
        public void ExpireAuth() => authExpired = true;

        /// <summary>Simulates successful re-authentication.</summary>
        public void RenewAuth() => authExpired = false;

        /// <summary>Current canonical revision for a stream, or zero when untouched.</summary>
        public long RevisionOf(string accountId, string streamKey)
            => TryGetStream(accountId, streamKey, out StreamState stream) ? stream.Revision : 0L;

        /// <summary>Current canonical payload for a stream.</summary>
        public string StateOf(string accountId, string streamKey)
            => TryGetStream(accountId, streamKey, out StreamState stream) ? stream.Payload : null;

        /// <summary>Clears all accounts, state, and counters.</summary>
        public void Reset()
        {
            accounts.Clear();
            AttemptCount = 0;
            offline = false;
            authExpired = false;
        }

        /// <summary>
        /// Processes one operation. Evaluation order is fixed and total: scripted fault, account
        /// existence, auth, deduplication, revision check, then apply.
        /// </summary>
        public SyncResult Process(string accountId, SyncOperation operation, string authToken)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            AttemptCount++;
            string correlation = operation.CorrelationId;

            SyncFaultKind fault = faultScript.ForAttempt(AttemptCount);
            switch (fault)
            {
                case SyncFaultKind.AuthExpired:
                    return SyncResult.AuthRequired(correlation);
                case SyncFaultKind.ProtocolError:
                    return SyncResult.Permanent("scripted-protocol-error", correlation);
                case SyncFaultKind.NetworkUnavailable:
                    return SyncResult.Retryable("scripted-network-unavailable", correlation);
                case SyncFaultKind.Timeout:
                    return SyncResult.Retryable("scripted-timeout", correlation);
                case SyncFaultKind.RejectBusinessRule:
                    return SyncResult.Rejected("scripted-business-rejection", correlation);
            }

            if (!accounts.TryGetValue(accountId, out Dictionary<string, StreamState> streams))
                return SyncResult.Permanent("unknown-account", correlation);

            if (authExpired || string.IsNullOrEmpty(authToken))
                return SyncResult.AuthRequired(correlation);

            if (!streams.TryGetValue(operation.StreamKey, out StreamState stream))
            {
                stream = new StreamState();
                streams[operation.StreamKey] = stream;
            }

            if (stream.AppliedOperations.TryGetValue(operation.OperationId, out SyncResult original))
                return SyncResult.Duplicate(original.CanonicalRevision, original.CanonicalPayload, correlation);

            if (fault == SyncFaultKind.ForceConflict || operation.ExpectedRevision != stream.Revision)
                return SyncResult.Conflict(stream.Revision, stream.Payload, correlation);

            // Payload is opaque: store it verbatim rather than interpreting domain meaning.
            stream.Revision++;
            stream.Payload = operation.Payload;

            SyncResult accepted = SyncResult.Accepted(stream.Revision, stream.Payload, correlation);
            stream.AppliedOperations[operation.OperationId] = accepted;
            return accepted;
        }

        private bool TryGetStream(string accountId, string streamKey, out StreamState stream)
        {
            stream = null;
            return accounts.TryGetValue(accountId, out Dictionary<string, StreamState> streams)
                   && streams.TryGetValue(streamKey, out stream);
        }
    }
}

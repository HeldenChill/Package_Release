using System;

namespace Hung.Sync.Simulator
{
    /// <summary>
    /// DEVELOPMENT AND TEST ONLY. Adapts <see cref="SyncSimulatorServer"/> to
    /// <see cref="ISyncTransport"/>, mapping simulator conditions onto transport outcomes.
    /// </summary>
    public sealed class SimulatorTransport : ISyncTransport
    {
        private readonly SyncSimulatorServer server;
        private readonly string accountId;

        /// <summary>Creates a transport bound to one simulated account.</summary>
        public SimulatorTransport(SyncSimulatorServer server, string accountId)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.accountId = string.IsNullOrWhiteSpace(accountId)
                ? throw new ArgumentException("Account id cannot be empty.", nameof(accountId))
                : accountId;
        }

        /// <inheritdoc />
        public SyncTransportResponse Send(SyncOperation operation, string authToken)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (server.IsOffline)
                return SyncTransportResponse.Failed(SyncTransportOutcome.NetworkUnavailable, "simulator-offline");

            SyncResult result = server.Process(accountId, operation, authToken);

            // An auth verdict is a transport-level condition, so the classifier treats it uniformly
            // whether it came from the simulator or from a real adapter.
            if (result.Kind == SyncResultKind.AuthenticationRequired)
                return SyncTransportResponse.Failed(SyncTransportOutcome.AuthExpired, "simulator-auth-expired");

            return SyncTransportResponse.Delivered(result);
        }
    }

    /// <summary>
    /// DEVELOPMENT AND TEST ONLY. Supplies a fake token that becomes unavailable once the
    /// simulator marks auth expired.
    /// </summary>
    public sealed class SimulatorAuthProvider : ISyncAuthProvider
    {
        private readonly SyncSimulatorServer server;

        /// <summary>Creates a provider bound to a simulator.</summary>
        public SimulatorAuthProvider(SyncSimulatorServer server)
            => this.server = server ?? throw new ArgumentNullException(nameof(server));

        /// <inheritdoc />
        public bool TryGetToken(out string token)
        {
            if (server.IsAuthExpired)
            {
                token = null;
                return false;
            }

            token = "simulator-token";
            return true;
        }

        /// <inheritdoc />
        public void InvalidateToken() => server.ExpireAuth();
    }
}

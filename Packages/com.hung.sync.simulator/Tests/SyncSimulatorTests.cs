using System;
using Hung.Sync;
using Hung.Sync.Simulator;
using NUnit.Framework;

namespace Hung.Sync.Simulator.Tests
{
    /// <summary>Gate G5: fault scripts are reproducible; dedup and revisions are deterministic.</summary>
    [TestFixture]
    public class SyncSimulatorTests
    {
        private const string Account = "acct-1";
        private const string Stream = "pvm.wallet";

        private static SyncOperation Operation(string id, long expectedRevision) =>
            new SyncOperation(id, Stream, expectedRevision, "wallet.earn", "{\"gold\":5}", "run.reward",
                new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc),
                SyncAuthority.OptimisticAllowed, "corr-" + id);

        private static SyncSimulatorServer NewServer(SyncFaultScript script = null)
        {
            var server = new SyncSimulatorServer(script);
            server.CreateAccount(Account);
            return server;
        }

        [Test]
        public void Process_FirstOperation_AcceptsAndAdvancesRevision()
        {
            SyncSimulatorServer server = NewServer();

            SyncResult result = server.Process(Account, Operation("op-1", 0), "token");

            Assert.AreEqual(SyncResultKind.Accepted, result.Kind);
            Assert.AreEqual(1L, result.CanonicalRevision);
        }

        // Gate G1 foundation: the same operation delivered repeatedly changes state once.
        [Test]
        public void Process_SameOperationIdTwice_DeduplicatesWithoutReapplying()
        {
            SyncSimulatorServer server = NewServer();
            SyncOperation op = Operation("op-dup", 0);

            SyncResult first = server.Process(Account, op, "token");
            SyncResult second = server.Process(Account, op, "token");

            Assert.AreEqual(SyncResultKind.Accepted, first.Kind);
            Assert.AreEqual(SyncResultKind.DuplicateAccepted, second.Kind);
            Assert.AreEqual(first.CanonicalRevision, second.CanonicalRevision);
            Assert.AreEqual(1L, server.RevisionOf(Account, Stream), "Revision must advance exactly once.");
        }

        [Test]
        public void Process_StaleExpectedRevision_ReturnsConflictWithCanonicalState()
        {
            SyncSimulatorServer server = NewServer();
            server.Process(Account, Operation("op-1", 0), "token");

            SyncResult conflict = server.Process(Account, Operation("op-2", 0), "token");

            Assert.AreEqual(SyncResultKind.RevisionConflict, conflict.Kind);
            Assert.AreEqual(1L, conflict.CanonicalRevision);
            Assert.IsNotNull(conflict.CanonicalPayload);
        }

        [Test]
        public void Process_WhenOffline_ReturnsRetryableThroughTransport()
        {
            SyncSimulatorServer server = NewServer();
            server.SetOffline(true);
            var transport = new SimulatorTransport(server, Account);

            SyncTransportResponse response = transport.Send(Operation("op-1", 0), "token");

            Assert.AreEqual(SyncTransportOutcome.NetworkUnavailable, response.Outcome);
            Assert.AreEqual(SyncResultKind.RetryableTransportFailure,
                SyncRetryClassifier.Classify(response));
        }

        [Test]
        public void Process_ExpiredAuth_ReturnsAuthenticationRequired()
        {
            SyncSimulatorServer server = NewServer();
            server.ExpireAuth();
            var transport = new SimulatorTransport(server, Account);

            SyncTransportResponse response = transport.Send(Operation("op-1", 0), "stale-token");

            Assert.AreEqual(SyncResultKind.AuthenticationRequired,
                SyncRetryClassifier.Classify(response));
            Assert.AreEqual(0L, server.RevisionOf(Account, Stream), "Auth failure must grant no value.");
        }

        [Test]
        public void FaultScript_ForcedConflictOnSecondAttempt_IsDeterministic()
        {
            var script = new SyncFaultScript();
            script.At(2, SyncFaultKind.ForceConflict);
            SyncSimulatorServer server = NewServer(script);

            SyncResult first = server.Process(Account, Operation("op-1", 0), "token");
            SyncResult second = server.Process(Account, Operation("op-2", 1), "token");

            Assert.AreEqual(SyncResultKind.Accepted, first.Kind);
            Assert.AreEqual(SyncResultKind.RevisionConflict, second.Kind);
        }

        // Gate G5: same script, same sequence, every run.
        [Test]
        public void FaultScript_RepeatedRuns_ProduceIdenticalResultSequences()
        {
            SyncResultKind[] RunOnce()
            {
                var script = new SyncFaultScript();
                script.At(1, SyncFaultKind.Timeout);
                script.At(3, SyncFaultKind.RejectBusinessRule);
                SyncSimulatorServer server = NewServer(script);

                return new[]
                {
                    server.Process(Account, Operation("a", 0), "token").Kind,
                    server.Process(Account, Operation("b", 0), "token").Kind,
                    server.Process(Account, Operation("c", 1), "token").Kind
                };
            }

            CollectionAssert.AreEqual(RunOnce(), RunOnce());
        }

        [Test]
        public void Reset_ClearsStateAndAttemptCount()
        {
            SyncSimulatorServer server = NewServer();
            server.Process(Account, Operation("op-1", 0), "token");

            server.Reset();
            server.CreateAccount(Account);

            Assert.AreEqual(0L, server.RevisionOf(Account, Stream));
            Assert.AreEqual(0, server.AttemptCount);
        }

        [Test]
        public void Process_UnknownAccount_ReturnsPermanentFailure()
        {
            SyncSimulatorServer server = NewServer();

            SyncResult result = server.Process("no-such-account", Operation("op-1", 0), "token");

            Assert.AreEqual(SyncResultKind.PermanentProtocolFailure, result.Kind);
        }
    }
}

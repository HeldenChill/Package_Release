using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Hung.Data.Tests.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Rewards
{
    public class RewardClaimCoordinatorTests
    {
        [Test]
        public void Claim_Success_PersistsGrantedClaim_ThenFinalizeMarksFinal()
        {
            Fixture fixture = new Fixture();
            var request = fixture.Request("claim-a");

            RewardClaimResult result = fixture.Coordinator.Claim(request);

            Assert.That(result.Outcome, Is.EqualTo(RewardGrantOutcome.Success));
            RewardClaimRecordData record = fixture.Reload("claim-a");
            Assert.That(record.state, Is.EqualTo((int)RewardClaimStateData.Granted));
            Assert.That(fixture.Grant.Calls.Single().ClaimId, Is.EqualTo(request.ClaimId));

            RewardClaimResult finalize = fixture.Coordinator.Finalize(request.ClaimId, () => new RewardFeatureCommitResult(true));

            Assert.That(finalize.Outcome, Is.EqualTo(RewardGrantOutcome.Success));
            Assert.That(fixture.Reload("claim-a").state, Is.EqualTo((int)RewardClaimStateData.Finalized));
        }

        [Test]
        public void Claim_SameIdDifferentPayload_ReturnsConflict()
        {
            Fixture fixture = new Fixture();
            fixture.Coordinator.Claim(fixture.Request("claim-a", "hash-a"));

            RewardClaimResult result = fixture.Coordinator.Claim(fixture.Request("claim-a", "hash-b"));

            Assert.That(result.Outcome, Is.EqualTo(RewardGrantOutcome.Conflict));
        }

        [Test]
        public void Recover_Granting_ReplaysSameId()
        {
            Fixture fixture = new Fixture();
            fixture.Seed("claim-a", RewardClaimStateData.Granting, "hash-a");
            fixture.Grant.NextOutcome = RewardGrantOutcome.IdempotentReplay;

            RewardRecoveryReport report = fixture.Coordinator.RecoverPending();

            Assert.That(report.RecoveredCount, Is.EqualTo(1));
            Assert.That(fixture.Grant.Calls.Single().ClaimId.Value, Is.EqualTo("claim-a"));
            Assert.That(fixture.Reload("claim-a").state, Is.EqualTo((int)RewardClaimStateData.Granted));
        }

        private sealed class Fixture
        {
            private readonly InMemorySaveStore store = new();
            private readonly SaveDefinition<RewardIntegrityStateData> definition;

            public Fixture()
            {
                definition = PackageSaveDefinitions.RewardIntegrity(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());
                Persistence = new PersistenceService(store);
                Clock = new FakeClock(new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc));
                Grant = new RecordingGrantService();
                Coordinator = new RewardClaimCoordinator(Clock, new RewardDayPolicy(0), "local-profile", Persistence, definition, Grant);
            }

            public PersistenceService Persistence { get; }
            public FakeClock Clock { get; }
            public RecordingGrantService Grant { get; }
            public RewardClaimCoordinator Coordinator { get; }

            public RewardClaimRequest Request(string claim, string fingerprint = "hash-a") =>
                new RewardClaimRequest(
                    new RewardClaimId(claim),
                    "daily-gift",
                    new List<RewardGrantItem> { new RewardGrantItem(BaseItemIds.Gold, 5) },
                    fingerprint);

            public void Seed(string claimId, RewardClaimStateData state, string fingerprint)
            {
                RewardIntegrityStateData data = RewardIntegrityStateData.CreateDefault();
                var record = RewardClaimRecordData.Prepared(claimId, "daily-gift", fingerprint, Clock.UtcNow.Ticks);
                record.state = (int)state;
                record.items.Add(new RewardGrantItemData(BaseItemIds.Gold.Value, 5));
                data.claims.Add(record);
                Assert.That(Persistence.Save(definition, data).Success, Is.True);
            }

            public RewardClaimRecordData Reload(string claimId)
            {
                return Persistence.Load(definition).Value.claims.Single(x => x.claimId == claimId);
            }
        }

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTime UtcNow { get; set; }
        }

        private sealed class RecordingGrantService : IRewardGrantService
        {
            public readonly List<(RewardClaimId ClaimId, IReadOnlyList<RewardGrantItem> Items, string Fingerprint)> Calls = new();
            public RewardGrantOutcome NextOutcome = RewardGrantOutcome.Success;

            public RewardGrantResult Grant(RewardClaimId id, IReadOnlyList<RewardGrantItem> items, string fingerprint)
            {
                Calls.Add((id, items, fingerprint));
                return new RewardGrantResult(NextOutcome);
            }
        }
    }
}

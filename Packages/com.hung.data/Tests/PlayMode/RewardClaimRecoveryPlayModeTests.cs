using System;
using System.IO;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.PlayModeTests
{
    public class RewardClaimRecoveryPlayModeTests
    {
        private string root;
        private PlainJsonSaveCodec codec;
        private Sha256SaveProtector protector;
        private SaveDefinition<RewardIntegrityStateData> definition;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath, "RewardClaimRecoveryTests", Guid.NewGuid().ToString("N"));
            codec = new PlainJsonSaveCodec();
            protector = new Sha256SaveProtector();
            definition = PackageSaveDefinitions.RewardIntegrity(codec, protector);
        }

        [TearDown]
        public void TearDown()
        {
            string fullRoot = Path.GetFullPath(root);
            string allowedRoot = Path.GetFullPath(Path.Combine(Application.temporaryCachePath, "RewardClaimRecoveryTests"));
            if (fullRoot.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
                Directory.Delete(fullRoot, true);
        }

        [Test]
        public void RecoverPending_GrantingRecord_ReplaysGrantOnce()
        {
            RewardClaimId id = RewardClaimId.Create("recovery", "granting", "20260727", "1", "gold", "profile");
            WriteState(RewardClaimStateData.Granting, id, "payload-a");
            CountingGrantService grant = new();

            RewardRecoveryReport report = CreateCoordinator(grant).RecoverPending();

            Assert.That(report.RecoveredCount, Is.EqualTo(1));
            Assert.That(report.FailedCount, Is.EqualTo(0));
            Assert.That(grant.Grants, Is.EqualTo(1));
            RewardIntegrityStateData state = LoadState();
            Assert.That(state.claims[0].state, Is.EqualTo((int)RewardClaimStateData.Granted));
        }

        [Test]
        public void RecoverPending_GrantedRecord_ReplaysGrantIdempotentlyWithoutSecondMutation()
        {
            RewardClaimId id = RewardClaimId.Create("recovery", "granted", "20260727", "1", "gold", "profile");
            WriteState(RewardClaimStateData.Granted, id, "payload-b");
            CountingGrantService grant = new();

            RewardRecoveryReport first = CreateCoordinator(grant).RecoverPending();
            RewardRecoveryReport second = CreateCoordinator(grant).RecoverPending();

            Assert.That(first.RecoveredCount, Is.EqualTo(1));
            Assert.That(second.RecoveredCount, Is.EqualTo(1));
            Assert.That(grant.UniqueMutations, Is.EqualTo(1));
        }

        [Test]
        public void FinalizeAfterRecoveredGrant_MarksLedgerFinalized()
        {
            RewardClaimId id = RewardClaimId.Create("recovery", "finalize", "20260727", "1", "gold", "profile");
            WriteState(RewardClaimStateData.Granted, id, "payload-c");
            RewardClaimCoordinator coordinator = CreateCoordinator(new CountingGrantService());

            RewardClaimResult result = coordinator.Finalize(id, () => new RewardFeatureCommitResult(true));

            Assert.That(result.Success, Is.True);
            Assert.That(LoadState().claims[0].state, Is.EqualTo((int)RewardClaimStateData.Finalized));
        }

        private RewardClaimCoordinator CreateCoordinator(IRewardGrantService grantService) =>
            new(new FixedClock(), new RewardDayPolicy(0), "profile", CreatePersistence(), definition, grantService);

        private PersistenceService CreatePersistence()
        {
            var service = new PersistenceService(new FileSaveStore(root));
            service.Register(definition);
            return service;
        }

        private void WriteState(RewardClaimStateData state, RewardClaimId id, string fingerprint)
        {
            RewardClaimRecordData record = RewardClaimRecordData.Prepared(id.Value, "recovery", fingerprint, new FixedClock().UtcNow.Ticks);
            record.state = (int)state;
            record.items.Add(new RewardGrantItemData(BaseItemIds.Gold.Value, 5));
            var value = new RewardIntegrityStateData();
            value.claims.Add(record);
            Assert.That(CreatePersistence().Save(definition, value).Success, Is.True);
        }

        private RewardIntegrityStateData LoadState()
        {
            LoadResult<RewardIntegrityStateData> load = CreatePersistence().Load(definition);
            Assert.That(load.Success, Is.True);
            return load.Value;
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        }

        private sealed class CountingGrantService : IRewardGrantService
        {
            private readonly System.Collections.Generic.HashSet<string> receiptIds = new();
            public int Grants { get; private set; }
            public int UniqueMutations { get; private set; }

            public RewardGrantResult Grant(RewardClaimId id, System.Collections.Generic.IReadOnlyList<RewardGrantItem> items, string fingerprint)
            {
                Grants++;
                if (receiptIds.Add(id.Value))
                {
                    UniqueMutations++;
                    return new RewardGrantResult(RewardGrantOutcome.Success);
                }

                return new RewardGrantResult(RewardGrantOutcome.IdempotentReplay);
            }
        }
    }
}

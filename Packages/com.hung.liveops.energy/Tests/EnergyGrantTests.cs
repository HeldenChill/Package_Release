using System;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    [TestFixture]
    internal sealed class EnergyGrantTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static EnergyConfig MakeConfig(
            int renewableMax = 10,
            int regenerationIntervalSeconds = 60,
            int initialRenewable = 5,
            int initialBonus = 0,
            int transactionRetentionCapacity = 20)
        {
            return new EnergyConfig(
                renewableMax,
                TimeSpan.FromSeconds(regenerationIntervalSeconds),
                1,
                initialRenewable,
                initialBonus,
                transactionRetentionCapacity);
        }

        private static EnergyService MakeService(
            out FakeClock clock,
            out InMemoryEnergyStateStore store,
            EnergyConfig config = null)
        {
            config ??= MakeConfig();
            clock = new FakeClock(BaseUtc);
            store = new InMemoryEnergyStateStore();
            FixedEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());
            return new EnergyService(clock, store, provider);
        }

        [Test]
        public void AddRenewable_MatchingReplay_ReturnsIdempotentReplay_NoDoubleApply()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(initialRenewable: 5));
            int changedCount = 0;
            service.Changed += _ => changedCount++;

            EnergyGrantResult first = service.AddRenewable(2, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.Success, first.Outcome);
            Assert.AreEqual(7, first.Snapshot.RenewableAmount);

            EnergyGrantResult second = service.AddRenewable(2, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, second.Outcome);
            Assert.AreEqual(7, second.Snapshot.RenewableAmount);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void AddRenewable_MismatchedPayload_ReturnsConflict_NoMutation()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(initialRenewable: 5));
            service.AddRenewable(2, "tx-1");

            EnergyGrantResult conflict = service.AddRenewable(3, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.Conflict, conflict.Outcome);
            Assert.AreEqual(7, conflict.Snapshot.RenewableAmount);
        }

        [Test]
        public void AddRenewable_MismatchedCommandKind_ReturnsConflict()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(initialRenewable: 5));
            service.AddRenewable(2, "tx-1");

            EnergyGrantResult conflict = service.AddBonus(2, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.Conflict, conflict.Outcome);
        }

        [Test]
        public void AddRenewable_CapsAtRenewableMax()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(renewableMax: 10, initialRenewable: 8));

            EnergyGrantResult result = service.AddRenewable(5, "tx-1");
            Assert.AreEqual(10, result.Snapshot.RenewableAmount);
        }

        [Test]
        public void AddBonus_MatchingReplay_And_MismatchedConflict()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(initialBonus: 0));

            EnergyGrantResult first = service.AddBonus(3, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.Success, first.Outcome);
            Assert.AreEqual(3, first.Snapshot.BonusAmount);

            EnergyGrantResult replay = service.AddBonus(3, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, replay.Outcome);
            Assert.AreEqual(3, replay.Snapshot.BonusAmount);

            EnergyGrantResult conflict = service.AddBonus(4, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.Conflict, conflict.Outcome);
        }

        [Test]
        public void AddBonus_IsUncapped_ExceedsRenewableMax()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(renewableMax: 10, initialBonus: 0));

            EnergyGrantResult result = service.AddBonus(50, "tx-1");
            Assert.AreEqual(50, result.Snapshot.BonusAmount);
        }

        [Test]
        public void AddRenewable_And_AddBonus_RejectNonPositiveAmount()
        {
            EnergyService service = MakeService(out _, out InMemoryEnergyStateStore store, MakeConfig());
            int changedCount = 0;
            service.Changed += _ => changedCount++;

            EnergyGrantResult renewableResult = service.AddRenewable(0, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.InvalidInput, renewableResult.Outcome);

            EnergyGrantResult bonusResult = service.AddBonus(-1, "tx-2");
            Assert.AreEqual(EnergyResultOutcome.InvalidInput, bonusResult.Outcome);

            Assert.AreEqual(0, store.SaveCallCount);
            Assert.AreEqual(0, changedCount);
        }

        [Test]
        public void GrantPersistenceFailure_AddRenewable_AddBonus_GrantUnlimited_LeavesStateUntouched()
        {
            foreach (bool useBonus in new[] { false, true })
            {
                EnergyService service = MakeService(out _, out InMemoryEnergyStateStore store, MakeConfig());
                int changedCount = 0;
                service.Changed += _ => changedCount++;
                EnergySnapshot before = service.Current;

                store.FailNextSave = true;
                EnergyGrantResult result = useBonus
                    ? service.AddBonus(2, "tx-fail")
                    : service.AddRenewable(2, "tx-fail");

                Assert.AreEqual(EnergyResultOutcome.PersistenceFailure, result.Outcome);
                Assert.AreEqual(before.RenewableAmount, service.Current.RenewableAmount);
                Assert.AreEqual(before.BonusAmount, service.Current.BonusAmount);
                Assert.AreEqual(0, changedCount);

                // Retry with same id after "fixing" the store must not be misread as a replay.
                EnergyGrantResult retry = useBonus
                    ? service.AddBonus(2, "tx-fail")
                    : service.AddRenewable(2, "tx-fail");
                Assert.AreEqual(EnergyResultOutcome.Success, retry.Outcome);
            }
        }

        [Test]
        public void GrantPersistenceFailure_GrantUnlimited_LeavesStateUntouched()
        {
            EnergyService service = MakeService(out _, out InMemoryEnergyStateStore store, MakeConfig());
            int changedCount = 0;
            service.Changed += _ => changedCount++;
            EnergySnapshot before = service.Current;

            store.FailNextSave = true;
            EnergyGrantResult result = service.GrantUnlimited(TimeSpan.FromMinutes(5), "tx-fail");

            Assert.AreEqual(EnergyResultOutcome.PersistenceFailure, result.Outcome);
            Assert.AreEqual(before.IsUnlimitedActive, service.Current.IsUnlimitedActive);
            Assert.AreEqual(0, changedCount);

            EnergyGrantResult retry = service.GrantUnlimited(TimeSpan.FromMinutes(5), "tx-fail");
            Assert.AreEqual(EnergyResultOutcome.Success, retry.Outcome);
        }

        [Test]
        public void Ledger_EvictsOldestBeyondRetentionCapacity()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(transactionRetentionCapacity: 3));

            service.AddRenewable(1, "tx-1");
            service.AddRenewable(1, "tx-2");
            service.AddRenewable(1, "tx-3");
            service.AddRenewable(1, "tx-4"); // evicts tx-1

            // Replaying tx-1's exact id+kind+fingerprint should now be treated as a NEW
            // transaction (fresh Success) since it was evicted from the ledger's memory.
            EnergyGrantResult replayEvicted = service.AddRenewable(1, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.Success, replayEvicted.Outcome);

            // tx-4, still within capacity, remains a true replay.
            EnergyGrantResult replayRecent = service.AddRenewable(1, "tx-4");
            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, replayRecent.Outcome);
        }

        [Test]
        public void FingerprintDeterminism_SameInput_SameHash_AcrossServiceInstances()
        {
            EnergyConfig config = MakeConfig();
            FakeClock clockA = new FakeClock(BaseUtc);
            InMemoryEnergyStateStore sharedStore = new InMemoryEnergyStateStore();
            FixedEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());

            EnergyService serviceA = new EnergyService(clockA, sharedStore, provider);
            EnergyGrantResult resultA = serviceA.AddRenewable(3, "tx-fp");
            Assert.AreEqual(EnergyResultOutcome.Success, resultA.Outcome);

            FakeClock clockB = new FakeClock(BaseUtc);
            EnergyService serviceB = new EnergyService(clockB, sharedStore, provider);

            // Same id + same command + same amount loaded from persisted state -> replay,
            // proving the fingerprint computed by the second, independently-constructed
            // service instance matches the one computed by the first.
            EnergyGrantResult resultB = serviceB.AddRenewable(3, "tx-fp");
            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, resultB.Outcome);
        }
    }
}

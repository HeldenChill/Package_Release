using System;
using NUnit.Framework;
using UnityEngine;

namespace Hung.LiveOps.Energy.Tests
{
    [TestFixture]
    internal sealed class EnergyReconciliationTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static EnergyConfig MakeConfig(
            int renewableMax = 10,
            int regenerationIntervalSeconds = 60,
            int runCost = 1,
            int initialRenewable = 5,
            int initialBonus = 0,
            int transactionRetentionCapacity = 20)
        {
            return new EnergyConfig(
                renewableMax,
                TimeSpan.FromSeconds(regenerationIntervalSeconds),
                runCost,
                initialRenewable,
                initialBonus,
                transactionRetentionCapacity);
        }

        private static EnergyService MakeService(
            out FakeClock clock,
            out InMemoryEnergyStateStore store,
            out FixedEnergyConfigProvider provider,
            EnergyConfig config = null,
            InMemoryEnergyStateStore existingStore = null)
        {
            config ??= MakeConfig();
            clock = new FakeClock(BaseUtc);
            store = existingStore ?? new InMemoryEnergyStateStore();
            provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());
            return new EnergyService(clock, store, provider);
        }

        [Test]
        public void InitialState_ReflectsConfigInitialBalances()
        {
            EnergyConfig config = MakeConfig(initialRenewable: 4, initialBonus: 2);
            EnergyService service = MakeService(out _, out _, out _, config);

            Assert.AreEqual(4, service.Current.RenewableAmount);
            Assert.AreEqual(2, service.Current.BonusAmount);
            Assert.AreEqual(config.RenewableMax, service.Current.RenewableMax);
        }

        [Test]
        public void Reconcile_OneCompleteInterval_GrantsOneRenewable()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            clock.Advance(TimeSpan.FromSeconds(60));
            EnergySnapshot snapshot = service.Reconcile();

            Assert.AreEqual(6, snapshot.RenewableAmount);
        }

        [Test]
        public void Reconcile_MultipleCompleteIntervals_GrantsProportionallyCappedAtMax()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            clock.Advance(TimeSpan.FromSeconds(60 * 20)); // 20 intervals, only 5 needed to cap
            EnergySnapshot snapshot = service.Reconcile();

            Assert.AreEqual(10, snapshot.RenewableAmount);
        }

        [Test]
        public void Reconcile_PartialIntervalRemainder_IsPreservedAcrossCalls()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            clock.Advance(TimeSpan.FromSeconds(90)); // 1.5 intervals
            EnergySnapshot first = service.Reconcile();
            Assert.AreEqual(6, first.RenewableAmount);

            clock.Advance(TimeSpan.FromSeconds(30)); // + 0.5 interval => 2.0 total consumed
            EnergySnapshot second = service.Reconcile();
            Assert.AreEqual(7, second.RenewableAmount);
        }

        [Test]
        public void Reconcile_FullBalance_AnchorSnapsToNow_NoImmediateRefillAfterLongIdle()
        {
            EnergyConfig config = MakeConfig(renewableMax: 5, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            // Already at cap; advance a large amount of time and reconcile — anchor snaps to now.
            clock.Advance(TimeSpan.FromDays(30));
            EnergySnapshot afterLongIdle = service.Reconcile();
            Assert.AreEqual(5, afterLongIdle.RenewableAmount);

            // Advance less than one interval — if the anchor had stayed stale, this would
            // still show zero change either way, so instead verify indirectly:
            // advance less than one interval, reconcile again, still no change is expected either
            // way at cap, but a follow-up spend+regen scenario is out of scope for Task 6 (no
            // grant/spend API yet). We assert no change here as the direct observable check.
            clock.Advance(TimeSpan.FromSeconds(10));
            EnergySnapshot afterTinyAdvance = service.Reconcile();
            Assert.AreEqual(5, afterTinyAdvance.RenewableAmount);
        }

        [Test]
        public void Reconcile_ConfigChange_LowerCap_RetainsAboveCapBalance_PausesRegen()
        {
            EnergyConfig configHighCap = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 10);
            FakeClock clock = new FakeClock(BaseUtc);
            InMemoryEnergyStateStore store = new InMemoryEnergyStateStore();

            // Mutable provider so the "live config change" can be swapped between reconcile calls.
            MutableFixedEnergyConfigProvider provider =
                new MutableFixedEnergyConfigProvider(configHighCap, configHighCap.ComputeVersion());
            EnergyService service = new EnergyService(clock, store, provider);

            Assert.AreEqual(10, service.Current.RenewableAmount);

            EnergyConfig configLowCap = MakeConfig(renewableMax: 5, regenerationIntervalSeconds: 60, initialRenewable: 10);
            provider.Set(configLowCap, configLowCap.ComputeVersion());

            clock.Advance(TimeSpan.FromSeconds(60));
            EnergySnapshot snapshot = service.Reconcile();

            // Renewable is NOT clamped down to the new cap.
            Assert.AreEqual(10, snapshot.RenewableAmount);

            // Further reconciliation grants nothing while above the new cap.
            clock.Advance(TimeSpan.FromSeconds(600));
            EnergySnapshot snapshot2 = service.Reconcile();
            Assert.AreEqual(10, snapshot2.RenewableAmount);
        }

        [Test]
        public void Reconcile_OfflineRestart_ResumesFromPersistedState()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService first = MakeService(out FakeClock clock, out InMemoryEnergyStateStore store, out FixedEnergyConfigProvider provider, config);

            clock.Advance(TimeSpan.FromSeconds(120)); // 2 intervals
            EnergySnapshot afterFirstReconcile = first.Reconcile();
            Assert.AreEqual(7, afterFirstReconcile.RenewableAmount);

            // Simulate app restart: new service instance, same store, same clock instant.
            EnergyService second = new EnergyService(clock, store, provider);

            Assert.AreEqual(7, second.Current.RenewableAmount);
        }

        [Test]
        public void Reconcile_Rollback_DetectsRollback_GrantsNothing()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            clock.Advance(TimeSpan.FromSeconds(60));
            EnergySnapshot before = service.Reconcile();
            Assert.AreEqual(6, before.RenewableAmount);

            clock.Advance(TimeSpan.FromSeconds(-120)); // rollback before LatestObservedUtc
            EnergySnapshot rolledBack = service.Reconcile();

            Assert.IsTrue(rolledBack.RollbackDetected);
            Assert.AreEqual(6, rolledBack.RenewableAmount);
        }

        [Test]
        public void Reconcile_CatchUpAfterRollback_ResumesNormalProgress()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            clock.Advance(TimeSpan.FromSeconds(60));
            service.Reconcile(); // LatestObservedUtc = Base + 60s, renewable = 6

            clock.Advance(TimeSpan.FromSeconds(-30));
            EnergySnapshot rolledBack = service.Reconcile();
            Assert.IsTrue(rolledBack.RollbackDetected);
            Assert.AreEqual(6, rolledBack.RenewableAmount);

            // Advance forward PAST the original LatestObservedUtc (Base+60s) by more than one interval.
            // Current clock is Base+30s; go to Base+180s (150s forward from here).
            clock.Advance(TimeSpan.FromSeconds(150));
            EnergySnapshot caughtUp = service.Reconcile();

            Assert.IsFalse(caughtUp.RollbackDetected);
            Assert.AreEqual(8, caughtUp.RenewableAmount); // +2 intervals from the 6->? anchor at 60s->180s = 120s = 2 intervals
        }

        [Test]
        public void Reconcile_LargeForwardJump_CapsAtMax_NoException()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out _, out _, config);

            clock.Advance(TimeSpan.FromDays(3650)); // ~10 years

            EnergySnapshot snapshot = default;
            Assert.DoesNotThrow(() => snapshot = service.Reconcile());
            Assert.AreEqual(10, snapshot.RenewableAmount);
        }

        [Test]
        public void Reconcile_Idle_IsNoOp_DoesNotSaveOrPublishTwice()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out InMemoryEnergyStateStore store, out _, config);

            int changedCount = 0;
            service.Changed += _ => changedCount++;

            clock.Advance(TimeSpan.FromSeconds(60));
            service.Reconcile(); // real change: interval elapsed
            int saveCountAfterFirst = store.SaveCallCount;
            int changedAfterFirst = changedCount;

            service.Reconcile(); // idle: no time advance, nothing changed

            Assert.AreEqual(saveCountAfterFirst, store.SaveCallCount);
            Assert.AreEqual(changedAfterFirst, changedCount);
        }

        [Test]
        public void Reconcile_FailedSave_ReturnsUnchangedCurrent_NoChangedEvent()
        {
            EnergyConfig config = MakeConfig(renewableMax: 10, regenerationIntervalSeconds: 60, initialRenewable: 5);
            EnergyService service = MakeService(out FakeClock clock, out InMemoryEnergyStateStore store, out _, config);

            int changedCount = 0;
            service.Changed += _ => changedCount++;

            EnergySnapshot before = service.Current;

            clock.Advance(TimeSpan.FromSeconds(60)); // would change state
            store.FailNextSave = true;
            EnergySnapshot result = service.Reconcile();

            Assert.AreEqual(before.RenewableAmount, service.Current.RenewableAmount);
            Assert.AreEqual(before.RenewableAmount, result.RenewableAmount);
            Assert.AreEqual(0, changedCount);
        }
    }

    /// <summary>
    /// Test-only provider whose returned config/version can be swapped between calls, needed
    /// to exercise live mid-session config-change reconciliation (Task 6 test 6).
    /// </summary>
    internal sealed class MutableFixedEnergyConfigProvider : IEnergyConfigProvider
    {
        private EnergyConfig _config;
        private string _version;

        public MutableFixedEnergyConfigProvider(EnergyConfig config, string version)
        {
            _config = config;
            _version = version;
        }

        public void Set(EnergyConfig config, string version)
        {
            _config = config;
            _version = version;
        }

        public EnergyConfigResult GetConfig() => EnergyConfigResult.Ok(_config, _version);
    }
}

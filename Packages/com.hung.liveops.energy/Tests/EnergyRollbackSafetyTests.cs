using System;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    /// <summary>
    /// Flow audit A3. Design section 12: "Unlimited Energy does not gain duration from the
    /// rollback" and section 7: "Clock rollback does not extend the entitlement". Run start and
    /// the snapshot compared the expiry with the raw device clock, so rolling the clock back kept
    /// runs free after the entitlement had really run out. Also: a config change was adopted
    /// before its reconcile was saved, so a failed save left new config over uncommitted state.
    /// </summary>
    /// <remarks>
    /// Expected-value reasoning: config is max 10, run cost 1, initial renewable 5. Unlimited is
    /// granted for 10 minutes at Base, reconciled at Base+9m (LatestObservedUtc = Base+9m), then
    /// the clock is set back one hour to Base-51m. Before the fix the expiry Base+10m is later
    /// than the rolled-back now, so the run was free; after it the entitlement is paused during
    /// rollback and the run costs 1. Renewable keeps regenerating beneath Unlimited (design
    /// section 7): 9 one-minute intervals take 5 to the cap of 10, and the run leaves 9.
    /// </remarks>
    [TestFixture]
    internal sealed class EnergyRollbackSafetyTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static EnergyConfig Config(int renewableMax = 10) =>
            new EnergyConfig(renewableMax, TimeSpan.FromSeconds(60), 1, 5, 0, 20);

        [Test]
        public void ClockRollback_DoesNotKeepUnlimitedRunsFree()
        {
            var clock = new FakeClock(BaseUtc);
            EnergyConfig config = Config();
            var service = new EnergyService(clock, new InMemoryEnergyStateStore(),
                new FixedEnergyConfigProvider(config, config.ComputeVersion()));
            service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-unlimited");
            clock.Advance(TimeSpan.FromMinutes(9));
            service.Reconcile();

            clock.Advance(TimeSpan.FromHours(-1));
            EnergyRunStartResult run = service.TryStartRun("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, run.Outcome);
            Assert.IsFalse(run.IsFreeRun, "A rolled-back clock must not stretch Unlimited Energy.");
            Assert.AreEqual(9, run.Snapshot.RenewableAmount);
            Assert.IsFalse(run.Snapshot.IsUnlimitedActive);
            Assert.IsTrue(run.Snapshot.RollbackDetected);
        }

        [Test]
        public void ClockCatchUp_ResumesRemainingUnlimited()
        {
            var clock = new FakeClock(BaseUtc);
            EnergyConfig config = Config();
            var service = new EnergyService(clock, new InMemoryEnergyStateStore(),
                new FixedEnergyConfigProvider(config, config.ComputeVersion()));
            service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-unlimited");
            clock.Advance(TimeSpan.FromMinutes(9));
            service.Reconcile();
            clock.Advance(TimeSpan.FromHours(-1));
            service.Reconcile();

            clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(30)); // Base+9m30s, past LatestObserved
            EnergyRunStartResult run = service.TryStartRun("run-1");

            Assert.IsTrue(run.IsFreeRun, "Entitlement is paused, not deleted, during rollback.");
        }

        [Test]
        public void ConfigChange_FailedSave_KeepsPreviousConfig()
        {
            var clock = new FakeClock(BaseUtc);
            EnergyConfig high = Config(renewableMax: 10);
            var provider = new MutableFixedEnergyConfigProvider(high, high.ComputeVersion());
            var store = new InMemoryEnergyStateStore();
            var service = new EnergyService(clock, store, provider);

            EnergyConfig low = Config(renewableMax: 3);
            provider.Set(low, low.ComputeVersion());
            clock.Advance(TimeSpan.FromSeconds(1));
            store.FailNextSave = true;
            EnergySnapshot afterFailed = service.Reconcile();

            Assert.AreEqual(10, afterFailed.RenewableMax, "Uncommitted reconcile must not adopt the new config.");

            EnergySnapshot afterRetry = service.Reconcile();
            Assert.AreEqual(3, afterRetry.RenewableMax, "The next successful reconcile adopts it.");
        }
    }
}

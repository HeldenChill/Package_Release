using System;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    [TestFixture]
    internal sealed class EnergyUnlimitedTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static EnergyConfig MakeConfig()
        {
            return new EnergyConfig(10, TimeSpan.FromSeconds(60), 1, 5, 0, 20);
        }

        private static EnergyService MakeService(out FakeClock clock, out InMemoryEnergyStateStore store)
        {
            EnergyConfig config = MakeConfig();
            clock = new FakeClock(BaseUtc);
            store = new InMemoryEnergyStateStore();
            FixedEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());
            return new EnergyService(clock, store, provider);
        }

        [Test]
        public void GrantUnlimited_First_SetsExpiryToNowPlusDuration()
        {
            EnergyService service = MakeService(out FakeClock clock, out _);

            EnergyGrantResult result = service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-1");

            Assert.AreEqual(EnergyResultOutcome.Success, result.Outcome);
            Assert.AreEqual(clock.UtcNow + TimeSpan.FromMinutes(10), result.Snapshot.UnlimitedExpiryUtc);
        }

        [Test]
        public void GrantUnlimited_MatchingReplay_ExpiryUnchanged()
        {
            EnergyService service = MakeService(out _, out _);

            EnergyGrantResult first = service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-1");
            EnergyGrantResult replay = service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-1");

            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, replay.Outcome);
            Assert.AreEqual(first.Snapshot.UnlimitedExpiryUtc, replay.Snapshot.UnlimitedExpiryUtc);
        }

        [Test]
        public void GrantUnlimited_MismatchedDuration_SameId_ReturnsConflict()
        {
            EnergyService service = MakeService(out _, out _);

            service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-1");
            EnergyGrantResult conflict = service.GrantUnlimited(TimeSpan.FromMinutes(20), "tx-1");

            Assert.AreEqual(EnergyResultOutcome.Conflict, conflict.Outcome);
        }

        [Test]
        public void GrantUnlimited_SecondDifferentId_ExtendsFromLaterOfNowOrExpiry()
        {
            EnergyService service = MakeService(out FakeClock clock, out _);

            EnergyGrantResult first = service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-1");
            DateTime firstExpiry = first.Snapshot.UnlimitedExpiryUtc.Value;

            clock.Advance(TimeSpan.FromMinutes(2)); // still active, now < firstExpiry
            EnergyGrantResult second = service.GrantUnlimited(TimeSpan.FromMinutes(5), "tx-2");

            // Extends from firstExpiry (later than now), not from now.
            Assert.AreEqual(firstExpiry + TimeSpan.FromMinutes(5), second.Snapshot.UnlimitedExpiryUtc);
        }

        [Test]
        public void GrantUnlimited_RejectsNonPositiveDuration()
        {
            EnergyService service = MakeService(out _, out InMemoryEnergyStateStore store);

            EnergyGrantResult result = service.GrantUnlimited(TimeSpan.Zero, "tx-1");
            Assert.AreEqual(EnergyResultOutcome.InvalidInput, result.Outcome);
            Assert.AreEqual(0, store.SaveCallCount);
        }

        [Test]
        public void GrantUnlimited_DuringClockRollback_UsesLatestObservedUtc_NotRolledBackNow()
        {
            EnergyService service = MakeService(out FakeClock clock, out _);

            clock.Advance(TimeSpan.FromSeconds(60));
            service.Reconcile(); // sets LatestObservedUtc = BaseUtc + 60s
            DateTime latestObserved = BaseUtc + TimeSpan.FromSeconds(60);

            clock.Advance(TimeSpan.FromSeconds(-30)); // rollback: now = BaseUtc + 30s < LatestObservedUtc

            EnergyGrantResult result = service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-1");

            Assert.AreEqual(EnergyResultOutcome.Success, result.Outcome);
            Assert.AreEqual(latestObserved + TimeSpan.FromMinutes(10), result.Snapshot.UnlimitedExpiryUtc);
        }
    }
}

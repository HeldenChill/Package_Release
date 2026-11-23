using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hung.Base;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;
using Hung.Data;

namespace Hung.LiveOps.SpinWheel.Tests
{
    public class SpinWheelIntegrityTests
    {
        [Test]
        public void Current_ResetsOnNewUtcDay_AndFreezesRollback()
        {
            ConfigureDatabase(new SpinWheelSaveData { isDoneSpinFreeToday = true, adsSpinToday = 2, rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc));
            SpinWheelManager manager = CreateManager(clock, new RecordingCoordinator(), Config());

            Assert.That(manager.Current.FreeAvailable, Is.True);
            Assert.That(manager.Current.AdsSpinToday, Is.EqualTo(0));

            clock.UtcNow = new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(manager.Current.NextResetUtc, Is.EqualTo(new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(manager.DayOfYear, Is.EqualTo(209));
        }

        [Test]
        public void CompleteSpin_RequiresPreparedSpin_AndFinalizesFreeCounter()
        {
            ConfigureDatabase(new SpinWheelSaveData { rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            RecordingCoordinator coordinator = new();
            SpinWheelManager manager = CreateManager(clock, coordinator, Config());

            Assert.That(manager.CompleteSpin("missing").Outcome, Is.EqualTo(RewardGrantOutcome.Unavailable));
            SpinStartResult start = manager.PrepareFreeSpin(0);
            RewardClaimResult complete = manager.CompleteSpin(start.SpinId);

            Assert.That(start.Success, Is.True);
            Assert.That(complete.Success, Is.True);
            Assert.That(manager.IsDoneSpinFreeToday, Is.True);
            Assert.That(coordinator.Claims, Is.EqualTo(1));
        }

        // BUG-261114-c587: a failed feature save returns PersistenceFailure, counters stay unchanged.
        [Test]
        public void CompleteSpin_FeatureSaveFails_ReturnsFailureAndKeepsCounters()
        {
            ConfigureDatabase(new SpinWheelSaveData { rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            SpinWheelManager manager = CreateManager(clock, new RecordingCoordinator(), Config());
            SpinStartResult start = manager.PrepareFreeSpin(0);
            Assert.That(start.Success, Is.True);
            Database.ServiceFactory = () => null;

            RewardClaimResult complete = default;
            Assert.DoesNotThrow(() => complete = manager.CompleteSpin(start.SpinId));
            Assert.That(complete.Outcome, Is.EqualTo(RewardGrantOutcome.PersistenceFailure));
            Assert.That(manager.IsDoneSpinFreeToday, Is.False);
        }

        [Test]
        public void PrepareAuthorizedSpin_RequiresSpinPlacementAuthorization()
        {
            ConfigureDatabase(new SpinWheelSaveData { isDoneSpinFreeToday = true, rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            SpinWheelManager manager = CreateManager(clock, new RecordingCoordinator(), Config());

            SpinStartResult wrong = manager.PrepareAuthorizedSpin(0, new RewardAuthorization("ad-1", Placement.DAILY_REWARD, clock.UtcNow, "ok"));
            SpinStartResult valid = manager.PrepareAuthorizedSpin(0, new RewardAuthorization("ad-2", Placement.SPIN, clock.UtcNow, "ok"));

            Assert.That(wrong.Outcome, Is.EqualTo(RewardGrantOutcome.InvalidReward));
            Assert.That(valid.Success, Is.True);
        }

        [TearDown]
        public void TearDown() => Database.ServiceFactory = null;

        private static SpinWheelManager CreateManager(IClock clock, IRewardClaimCoordinator coordinator, SpinWheelDataSO config)
        {
            var go = new GameObject("SpinWheelIntegrityTest");
            SpinWheelManager manager = go.AddComponent<SpinWheelManager>();
            manager.ConfigureTimeRewardIntegrity(clock, new RewardDayPolicy(config.ResetOffsetMinutes), coordinator, "profile-a", config);
            return manager;
        }

        private static SpinWheelDataSO Config()
        {
            SpinWheelDataSO config = ScriptableObject.CreateInstance<SpinWheelDataSO>();
            SetField(config, "spinFreeItems", new List<SpinItem> { Reward(BaseItemIds.Gold, 5) });
            SetField(config, "spinAdsItems", new List<SpinItem> { Reward(BaseItemIds.Gold, 10) });
            SetField(config, "adsSpinPerDay", 2);
            return config;
        }

        private static SpinItem Reward(ItemId itemId, int value)
        {
            var reward = new SpinItem { value = value, rate = 1f };
            SetField(reward, "itemId", itemId);
            return reward;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void ConfigureDatabase(SpinWheelSaveData data)
        {
            var service = new PersistenceService(new FileSaveStore(Path.Combine(Path.GetTempPath(), "SpinWheelIntegrityTests", Guid.NewGuid().ToString("N"))));
            service.Register(PackageSaveDefinitions.SpinWheel(new PlainJsonSaveCodec(), new Sha256SaveProtector()));
            Database.Service = service;
            Database.Save(data);
        }

        private sealed class MutableClock : IClock
        {
            public MutableClock(DateTime utcNow) => UtcNow = utcNow;
            public DateTime UtcNow { get; set; }
        }

        private sealed class RecordingCoordinator : IRewardClaimCoordinator
        {
            public int Claims { get; private set; }

            public RewardClaimResult Claim(RewardClaimRequest request)
            {
                Claims++;
                return new RewardClaimResult(RewardGrantOutcome.Success);
            }

            public RewardClaimResult Finalize(RewardClaimId id, Func<RewardFeatureCommitResult> persistFeatureState)
            {
                RewardFeatureCommitResult result = persistFeatureState();
                return new RewardClaimResult(result.Success ? RewardGrantOutcome.Success : RewardGrantOutcome.PersistenceFailure);
            }

            public RewardRecoveryReport RecoverPending() => new RewardRecoveryReport(0, 0);
        }
    }
}

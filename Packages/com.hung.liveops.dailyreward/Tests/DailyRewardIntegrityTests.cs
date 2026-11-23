using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hung.Base;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;
using Hung.Data;

namespace Hung.LiveOps.DailyReward.Tests
{
    public class DailyRewardIntegrityTests
    {
        [Test]
        public void FreeCooldown_UsesAbsoluteUtcAcrossMidnight()
        {
            ConfigureDatabase(new DailyRewardSaveData { rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 23, 45, 0, DateTimeKind.Utc));
            DailyRewardManager manager = CreateManager(clock, new RecordingCoordinator(), Config());

            Assert.That(manager.ClaimFree().Success, Is.True);
            clock.UtcNow = new DateTime(2026, 7, 28, 0, 15, 0, DateTimeKind.Utc);

            Assert.That(manager.CanClaimFree, Is.False);
            Assert.That(manager.GetFreeCooldownRemaining(), Is.EqualTo(TimeSpan.FromMinutes(30)));
        }

        [Test]
        public void Reconcile_ResetsProgressOnNewUtcDay_AndFreezesRollback()
        {
            ConfigureDatabase(new DailyRewardSaveData { currentProgress = 3, rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc));
            DailyRewardManager manager = CreateManager(clock, new RecordingCoordinator(), Config());

            manager.Reconcile();

            Assert.That(manager.Current.Progress, Is.EqualTo(0));
            clock.UtcNow = new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc);
            manager.Reconcile();
            Assert.That(manager.Current.Day.Value, Is.EqualTo(20260728));
        }

        [Test]
        public void ClaimSequence_AdsRequiresDailyRewardAuthorization()
        {
            ConfigureDatabase(new DailyRewardSaveData { rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            RecordingCoordinator coordinator = new();
            DailyRewardManager manager = CreateManager(clock, coordinator, Config(adSlot: true));

            RewardClaimResult missing = manager.ClaimSequence(0);
            RewardClaimResult wrongPlacement = manager.ClaimSequence(0, new RewardAuthorization("ad-1", Placement.SPIN, clock.UtcNow, "ok"));
            RewardClaimResult accepted = manager.ClaimSequence(0, new RewardAuthorization("ad-2", Placement.DAILY_REWARD, clock.UtcNow, "ok"));

            Assert.That(missing.Outcome, Is.EqualTo(RewardGrantOutcome.Unavailable));
            Assert.That(wrongPlacement.Outcome, Is.EqualTo(RewardGrantOutcome.InvalidReward));
            Assert.That(accepted.Success, Is.True);
            Assert.That(coordinator.Claims, Is.EqualTo(1));
        }

        [Test]
        public void ClaimFree_RetryAfterUnfinishedFinalize_ReusesClaimId()
        {
            ConfigureDatabase(new DailyRewardSaveData { rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            RecordingCoordinator coordinator = new() { SkipFeatureCommit = true };
            DailyRewardManager manager = CreateManager(clock, coordinator, Config());

            manager.ClaimFree();
            clock.UtcNow = clock.UtcNow.AddSeconds(5);
            manager.ClaimFree();

            Assert.That(coordinator.Ids.Count, Is.EqualTo(2));
            Assert.That(coordinator.Ids[1], Is.EqualTo(coordinator.Ids[0]), "A retry before the cooldown commit must reuse the claim id so the coordinator dedupes it.");
        }

        [Test]
        public void ClaimFree_NextCooldownWindow_GetsNewClaimId()
        {
            ConfigureDatabase(new DailyRewardSaveData { rewardDayKey = 20260727, dayOfYear = 208 });
            MutableClock clock = new(new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            RecordingCoordinator coordinator = new();
            DailyRewardManager manager = CreateManager(clock, coordinator, Config());

            manager.ClaimFree();
            clock.UtcNow = clock.UtcNow.AddHours(2);
            manager.ClaimFree();

            Assert.That(coordinator.Ids.Count, Is.EqualTo(2));
            Assert.That(coordinator.Ids[1], Is.Not.EqualTo(coordinator.Ids[0]));
        }

        [TearDown]
        public void TearDown()
        {
            Database.ServiceFactory = null;
        }

        private static DailyRewardManager CreateManager(IClock clock, IRewardClaimCoordinator coordinator, DailyRewardDataSO config)
        {
            var go = new GameObject("DailyRewardIntegrityTest");
            DailyRewardManager manager = go.AddComponent<DailyRewardManager>();
            manager.ConfigureTimeRewardIntegrity(clock, new RewardDayPolicy(config.ResetOffsetMinutes), coordinator, "profile-a", config);
            return manager;
        }

        private static DailyRewardDataSO Config(bool adSlot = false)
        {
            DailyRewardDataSO config = ScriptableObject.CreateInstance<DailyRewardDataSO>();
            SetField(config, "freeRewardItem", Reward(BaseItemIds.Gold, 5));
            SetField(config, "rewardItems", new List<DailyRewardItem>
            {
                Reward(BaseItemIds.Gold, 10, adSlot),
                Reward(BaseItemIds.Gold, 20)
            });
            SetField(config, "freeRewardCooldownSeconds", 3600);
            return config;
        }

        private static DailyRewardItem Reward(ItemId itemId, int value, bool isAdsReward = false)
        {
            var reward = new DailyRewardItem { value = value, isAdsReward = isAdsReward };
            SetField(reward, "itemId", itemId);
            return reward;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void ConfigureDatabase(DailyRewardSaveData data)
        {
            var service = new PersistenceService(new FileSaveStore(Path.Combine(Path.GetTempPath(), "DailyRewardIntegrityTests", Guid.NewGuid().ToString("N"))));
            service.Register(PackageSaveDefinitions.DailyReward(new PlainJsonSaveCodec(), new Sha256SaveProtector()));
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
            public List<RewardClaimId> Ids { get; } = new List<RewardClaimId>();
            // Simulates a crash after the grant and before the feature state (cooldown) is saved.
            public bool SkipFeatureCommit { get; set; }

            public RewardClaimResult Claim(RewardClaimRequest request)
            {
                Claims++;
                Ids.Add(request.ClaimId);
                return new RewardClaimResult(RewardGrantOutcome.Success);
            }

            public RewardClaimResult Finalize(RewardClaimId id, Func<RewardFeatureCommitResult> persistFeatureState)
            {
                if (SkipFeatureCommit)
                    return new RewardClaimResult(RewardGrantOutcome.PersistenceFailure);
                RewardFeatureCommitResult result = persistFeatureState();
                return new RewardClaimResult(result.Success ? RewardGrantOutcome.Success : RewardGrantOutcome.PersistenceFailure);
            }

            public RewardRecoveryReport RecoverPending() => new RewardRecoveryReport(0, 0);
        }
    }
}

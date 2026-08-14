using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hung.Base;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Hung.LiveOps.DailyGift.Tests
{
    public class DailyGiftClaimTests
    {
        [Test]
        public void ClaimDailyGift_WithCoordinator_FinalizesSlotWithoutLocatorGrant()
        {
            ConfigureDatabase();
            RecordingCoordinator coordinator = new();
            DailyGiftManager manager = CreateManager(coordinator);

            manager.ClaimDailyGift(0);

            Assert.That(manager.DataModel.listDailyGiftStatus[0], Is.True);
            Assert.That(coordinator.Claims, Is.EqualTo(1));
            Assert.That(coordinator.Finalizes, Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            Database.ServiceFactory = null;
        }

        private static DailyGiftManager CreateManager(IRewardClaimCoordinator coordinator)
        {
            var go = new GameObject("DailyGiftClaimTest");
            DailyGiftManager manager = go.AddComponent<DailyGiftManager>();
            SetField(manager, "configModel", Config());
            SetField(manager, "dataModel", new DailyGiftDbModel
            {
                year = 2026,
                dayOfYear = 208,
                dayCount = 1,
                rewardDayKey = 20260727,
                cycleStartDayKey = 20260727,
                currentSlot = 0,
                listDailyGiftStatus = Status(7),
                listStreakDailyGiftStatus = Status(7)
            });
            manager.ConfigureTimeRewardIntegrity(new FakeClock(), new RewardDayPolicy(0), coordinator, "profile-a");
            return manager;
        }

        private static DailyGiftTestConfig Config()
        {
            DailyGiftTestConfig config = ScriptableObject.CreateInstance<DailyGiftTestConfig>();
            for (int i = 0; i < 7; i++)
            {
                var day = new DailyGiftTestDay { day = i };
                day.rewards.Add(new DailyGiftTestReward { itemId = BaseItemIds.Gold, quantity = 1 });
                config.daily.Add(day);
                config.streak.Add(day);
            }
            return config;
        }

        private static List<bool> Status(int count)
        {
            var list = new List<bool>();
            for (int i = 0; i < count; i++) list.Add(false);
            return list;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = typeof(DailyGiftManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void ConfigureDatabase()
        {
            var service = new PersistenceService(new FileSaveStore(Path.Combine(Path.GetTempPath(), "DailyGiftClaimTests", Guid.NewGuid().ToString("N"))));
            service.Register(PackageSaveDefinitions.DailyGift(new PlainJsonSaveCodec(), new Sha256SaveProtector()));
            Database.Service = service;
        }

        private sealed class FakeClock : IClock
        {
            public DateTime UtcNow => new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed class RecordingCoordinator : IRewardClaimCoordinator
        {
            public int Claims { get; private set; }
            public int Finalizes { get; private set; }

            public RewardClaimResult Claim(RewardClaimRequest request)
            {
                Claims++;
                return new RewardClaimResult(RewardGrantOutcome.Success);
            }

            public RewardClaimResult Finalize(RewardClaimId id, Func<RewardFeatureCommitResult> persistFeatureState)
            {
                Finalizes++;
                RewardFeatureCommitResult result = persistFeatureState();
                return new RewardClaimResult(result.Success ? RewardGrantOutcome.Success : RewardGrantOutcome.PersistenceFailure);
            }

            public RewardRecoveryReport RecoverPending() => new RewardRecoveryReport(0, 0);
        }
    }
}

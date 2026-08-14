using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.LiveOps.DailyGift.Tests
{
    public class DailyGiftCalendarTests
    {
        [Test]
        public void Reconcile_MultiDayAbsence_AdvancesByElapsedRewardDaysAndResetsStreak()
        {
            DailyGiftManager manager = CreateManager(
                new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc),
                new DailyGiftDbModel
                {
                    year = 2026,
                    dayOfYear = 208,
                    dayCount = 1,
                    rewardDayKey = 20260727,
                    cycleStartDayKey = 20260727,
                    currentSlot = 0,
                    streakDay = 2,
                    listDailyGiftStatus = Status(7),
                    listStreakDailyGiftStatus = Status(7)
                });

            bool changed = manager.Reconcile();

            Assert.That(changed, Is.True);
            Assert.That(manager.DataModel.rewardDayKey, Is.EqualTo(20260730));
            Assert.That(manager.DataModel.dayCount, Is.EqualTo(4));
            Assert.That(manager.DataModel.currentSlot, Is.EqualTo(3));
            Assert.That(manager.DataModel.streakDay, Is.EqualTo(0));
        }

        [Test]
        public void Reconcile_BackwardClock_DoesNotMutate()
        {
            DailyGiftManager manager = CreateManager(
                new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc),
                new DailyGiftDbModel
                {
                    year = 2026,
                    dayOfYear = 208,
                    dayCount = 2,
                    rewardDayKey = 20260727,
                    cycleStartDayKey = 20260727,
                    currentSlot = 1,
                    streakDay = 1,
                    listDailyGiftStatus = Status(7),
                    listStreakDailyGiftStatus = Status(7)
                });

            bool changed = manager.Reconcile();

            Assert.That(changed, Is.False);
            Assert.That(manager.DataModel.rewardDayKey, Is.EqualTo(20260727));
            Assert.That(manager.DataModel.dayCount, Is.EqualTo(2));
        }

        private static DailyGiftManager CreateManager(DateTime utcNow, DailyGiftDbModel model)
        {
            var go = new GameObject("DailyGiftManagerTest");
            DailyGiftManager manager = go.AddComponent<DailyGiftManager>();
            SetField(manager, "configModel", Config());
            SetField(manager, "dataModel", model);
            manager.ConfigureTimeRewardIntegrity(new FakeClock(utcNow), new RewardDayPolicy(0), null, "test-profile");
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

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTime utcNow) => UtcNow = utcNow;
            public DateTime UtcNow { get; }
        }
    }
}

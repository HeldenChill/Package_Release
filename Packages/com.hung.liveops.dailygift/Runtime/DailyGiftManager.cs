using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Hung.LiveOps.DailyGift
{
    using Hung.Base;
    using Hung.Data.LiveOps;
    using Hung.DesignPattern;
    public class DailyGiftManager : Singleton<DailyGiftManager>, IDailyGiftService
    {
        private IClock clock = new SystemClock();
        private RewardDayPolicy dayPolicy = new(0);
        private IRewardClaimCoordinator rewardCoordinator;
        private string profileScope = "local-profile";

        public void ConfigureTimeRewardIntegrity(
            IClock clock,
            RewardDayPolicy dayPolicy,
            IRewardClaimCoordinator rewardCoordinator,
            string profileScope)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.dayPolicy = dayPolicy;
            this.rewardCoordinator = rewardCoordinator;
            this.profileScope = string.IsNullOrWhiteSpace(profileScope) ? "local-profile" : profileScope;
        }

        [HideInInspector] public bool initialized = false;
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(IEStart());
            Locator.DailyGift = this;
        }
        private IEnumerator IEStart()
        {
            initialized = false;
            ReloadData();
            yield return new WaitForEndOfFrame();
            UpdateNotify();
            yield return new WaitForEndOfFrame();
            StartCoroutine(IEResetDataNewDay());
            yield return new WaitForEndOfFrame();
            initialized = true;
        }

        private IEnumerator IEResetDataNewDay()
        {
            while (true)
            {
                if (Reconcile())
                    dataModel.Save();
                yield return new WaitForSeconds(1f);
            }
        }

        public bool Reconcile()
        {
            RewardDayKey currentDay = dayPolicy.Resolve(clock.UtcNow);
            EnsureRewardDayState(currentDay);

            if (dataModel.rewardDayKey > currentDay.Value)
                return false;

            RewardDayKey previousDay = new(dataModel.rewardDayKey);
            int deltaDays = (currentDay.ToUtcDate() - previousDay.ToUtcDate()).Days;
            if (deltaDays <= 0)
                return false;

            if (deltaDays == 1)
            {
                dataModel.streakDay++;
            }
            else
            {
                dataModel.streakDay = 0;
                dataModel.listStreakDailyGiftStatus = CreateStatusList(Config.DailyGifts.Count);
            }

            dataModel.rewardDayKey = currentDay.Value;
            DateTime currentUtcDate = currentDay.ToUtcDate();
            dataModel.year = currentUtcDate.Year;
            dataModel.dayOfYear = currentUtcDate.DayOfYear;
            dataModel.dayCount += deltaDays;
            dataModel.currentSlot = (dataModel.dayCount - 1) % 7;
            int cycleStartDayCount = dataModel.dayCount - dataModel.currentSlot;
            if (dataModel.cycleStartDayKey == 0 || cycleStartDayCount != dataModel.dayCount - deltaDays - ((dataModel.dayCount - deltaDays - 1) % 7))
            {
                dataModel.cycleStartDayKey = currentDay.ToUtcDate().AddDays(-dataModel.currentSlot).Year * 10000
                                             + currentDay.ToUtcDate().AddDays(-dataModel.currentSlot).Month * 100
                                             + currentDay.ToUtcDate().AddDays(-dataModel.currentSlot).Day;
                dataModel.listDailyGiftStatus = CreateStatusList(Config.DailyGifts.Count);
            }
            dataModel.lastStreakDayKey = currentDay.Value;
            InvokeCallbackDailyGift();
            return true;
        }

        [HideInInspector] public bool hasNotify = false;
        private void UpdateNotify()
        {
            hasNotify = false;
            for (var i = 0; i <= (dataModel.dayCount - 1) % 7; i++)
            {
                if (!dataModel.listDailyGiftStatus[i])
                {
                    hasNotify = true;
                    break;
                }
            }
        }
        private int _callbackDailyGiftIndex = 0;
        private Dictionary<int, System.Action> _callbackDailyGift = new();
        public void InvokeCallbackDailyGift()
        {
            UpdateNotify();
            var listCallbackInactive = new List<int>();
            foreach (var callback in _callbackDailyGift)
            {
                if (callback.Value != null) callback.Value?.Invoke();
                else listCallbackInactive.Add(callback.Key);
            }
            foreach (var index in listCallbackInactive)
                _callbackDailyGift.Remove(index);
        }
        public void ClaimDailyGift(int day)
        {
            ClaimGift(DailyGiftTrack.Normal, day);
        }

        private bool ClaimGift(DailyGiftTrack track, int day)
        {
            List<bool> statusList = GetGiftStatusList(track);
            if (statusList == null || day < 0 || day >= statusList.Count || statusList[day] || GetUnlockedDayIndex(track) < day) return false;
            IReadOnlyList<IDailyGiftDay> giftList = track switch
            {
                DailyGiftTrack.Normal => Config.DailyGifts,
                DailyGiftTrack.Streak => Config.StreakLoginGifts,
                _ => null
            };
            if (giftList == null || day >= giftList.Count) return false;

            if (rewardCoordinator == null)
                return false;

            if (rewardCoordinator != null)
            {
                string trackName = track.ToString().ToLowerInvariant();
                RewardClaimId id = RewardClaimId.Create("daily-gift", trackName, dataModel.cycleStartDayKey.ToString(), day.ToString(), "daily-gift-slot-" + day, profileScope);
                RewardClaimRequest request = new(id, "daily-gift", ToGrantItems(giftList[day].Rewards), "daily-gift:" + trackName + ":" + dataModel.cycleStartDayKey + ":" + day);
                RewardClaimResult claim = rewardCoordinator.Claim(request);
                if (!claim.Success) return false;
                RewardClaimResult finalize = rewardCoordinator.Finalize(id, () =>
                {
                    statusList[day] = true;
                    dataModel.Save();
                    return new RewardFeatureCommitResult(true);
                });
                if (!finalize.Success) return false;
            }
            InvokeCallbackDailyGift();
            return true;
        }

        [ContextMenu("NextDay")]
        public void NextDay()
        {
            dataModel.dayOfYear = -1;
            dataModel.Save();
        }

        [SerializeField]
        protected ScriptableObject configModel;

        private IDailyGiftConfig Config
        {
            get
            {
                if (configModel is IDailyGiftConfig config)
                    return config;

                throw new InvalidOperationException(
                    $"{nameof(DailyGiftManager)} requires a ScriptableObject implementing {nameof(IDailyGiftConfig)}.");
            }
        }

        [HideInInspector] protected DailyGiftDbModel dataModel;
        public DailyGiftDbModel DataModel => dataModel;

        [ContextMenu("ResetData")]
        public void ResetData()
        {
            dataModel ??= new DailyGiftDbModel();
            RewardDayKey currentDay = dayPolicy.Resolve(clock.UtcNow);
            DateTime currentUtcDate = currentDay.ToUtcDate();
            dataModel.year = currentUtcDate.Year;
            dataModel.dayOfYear = currentUtcDate.DayOfYear;
            dataModel.rewardDayKey = currentDay.Value;
            dataModel.cycleStartDayKey = currentDay.Value;
            dataModel.currentSlot = 0;
            dataModel.lastStreakDayKey = currentDay.Value;
            dataModel.dayCount = 1;
            dataModel.listDailyGiftStatus = CreateStatusList(Config.DailyGifts.Count);
            dataModel.listStreakDailyGiftStatus = CreateStatusList(Config.DailyGifts.Count);
            dataModel.Save();
            InvokeCallbackDailyGift();
        }
        private void ReloadData()
        {
            dataModel = DailyGiftDbModel.Load();
            if (dataModel.year == 0) ResetData();
            EnsureRewardDayState(dayPolicy.Resolve(clock.UtcNow));
            dataModel.Save();
        }

        private void EnsureRewardDayState(RewardDayKey fallbackDay)
        {
            dataModel.listDailyGiftStatus ??= CreateStatusList(Config.DailyGifts.Count);
            dataModel.listStreakDailyGiftStatus ??= CreateStatusList(Config.DailyGifts.Count);
            if (dataModel.rewardDayKey != 0) return;

            if (dataModel.year > 0 && dataModel.dayOfYear > 0)
            {
                DateTime legacyDate = new DateTime(dataModel.year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(dataModel.dayOfYear - 1);
                dataModel.rewardDayKey = RewardDayKey.FromUtcDate(legacyDate).Value;
            }
            else
            {
                dataModel.rewardDayKey = fallbackDay.Value;
            }

            dataModel.currentSlot = Mathf.Max(0, (dataModel.dayCount - 1) % 7);
            DateTime cycleStart = new RewardDayKey(dataModel.rewardDayKey).ToUtcDate().AddDays(-dataModel.currentSlot);
            dataModel.cycleStartDayKey = RewardDayKey.FromUtcDate(cycleStart).Value;
            dataModel.lastStreakDayKey = dataModel.rewardDayKey;
        }

        private static List<bool> CreateStatusList(int count)
        {
            var list = new List<bool>();
            for (int i = 0; i < count; i++)
                list.Add(false);
            return list;
        }

        public bool IsInitialized() => initialized;

        private List<bool> GetGiftStatusList(DailyGiftTrack track)
        {
            return track switch
            {
                DailyGiftTrack.Normal => dataModel.listDailyGiftStatus,
                DailyGiftTrack.Streak => dataModel.listStreakDailyGiftStatus,
                _ => null
            };
        }

        private int GetUnlockedDayIndex(DailyGiftTrack track)
        {
            return track switch
            {
                DailyGiftTrack.Normal => (dataModel.dayCount - 1) % 7,
                DailyGiftTrack.Streak => dataModel.streakDay,
                _ => 0
            };
        }

        public bool IsClaimed(DailyGiftTrack track, int day)
        {
            List<bool> statusList = GetGiftStatusList(track);
            if (statusList == null || day < 0 || day >= statusList.Count) return false;
            return statusList[day];
        }

        public bool CanClaim(DailyGiftTrack track, int day)
        {
            List<bool> statusList = GetGiftStatusList(track);
            if (statusList == null || day < 0 || day >= statusList.Count) return false;
            bool unlocked = day <= GetUnlockedDayIndex(track);
            return unlocked && !statusList[day];
        }

        public bool IsLocked(DailyGiftTrack track, int day)
        {
            List<bool> statusList = GetGiftStatusList(track);
            if (statusList == null) return true;
            return day > GetUnlockedDayIndex(track);
        }

        private static IReadOnlyList<RewardGrantItem> ToGrantItems(IReadOnlyList<IDailyGiftReward> rewards)
        {
            var items = new List<RewardGrantItem>();
            foreach (IDailyGiftReward reward in rewards)
            {
                if (!reward.ItemId.IsValid)
                    throw new InvalidOperationException("Daily gift reward is missing ItemId. Run ItemId asset migration.");
                items.Add(new RewardGrantItem(reward.ItemId, reward.Quantity));
            }
            return items;
        }

        public List<int> GetClaimableGiftDays(DailyGiftTrack track)
        {
            var result = new List<int>();
            List<bool> statusList = GetGiftStatusList(track);
            if (statusList == null) return result;

            int maxDay = Mathf.Min(GetUnlockedDayIndex(track), statusList.Count - 1);
            for (int i = 0; i <= maxDay; i++)
                if (!statusList[i]) result.Add(i);
            return result;
        }

        public void ClaimAllAvailableGifts(DailyGiftTrack track)
        {
            List<int> claimableDays = GetClaimableGiftDays(track);
            if (claimableDays.Count == 0) return;

            List<bool> statusList = GetGiftStatusList(track);
            if (statusList == null) return;

            IReadOnlyList<IDailyGiftDay> giftList = track switch
            {
                DailyGiftTrack.Normal => Config.DailyGifts,
                DailyGiftTrack.Streak => Config.StreakLoginGifts,
                _ => null
            };
            if (giftList == null) return;

            foreach (int day in claimableDays)
            {
                ClaimGift(track, day);
            }

            InvokeCallbackDailyGift();
        }
    }
}

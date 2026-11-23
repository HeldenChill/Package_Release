using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.DesignPattern;
using UnityEngine;
using Hung.Data;

namespace Hung.LiveOps.DailyReward
{
    public class DailyRewardManager : Singleton<DailyRewardManager>, IDailyRewardService
    {
        private void OnDestroy() => ServiceSlots.Unregister<IDailyRewardService>(this);

        [SerializeField] private DailyRewardDataSO rewardData;

        private IClock clock = new SystemClock();
        private RewardDayPolicy dayPolicy = new(0);
        private IRewardClaimCoordinator rewardCoordinator;
        private string profileScope = "local-profile";
        private DailyRewardSaveData saveData;

        private DailyRewardSaveData SaveData
        {
            get
            {
                if (saveData == null)
                {
                    saveData = Database.Load<DailyRewardSaveData>();
                    Reconcile();
                    Database.Save(saveData);
                }

                return saveData;
            }
        }

        public DailyRewardSnapshot Current
        {
            get
            {
                Reconcile();
                var slots = new DailyRewardSlotSnapshot[rewardData == null ? 0 : rewardData.TotalRewardItems];
                for (int i = 0; i < slots.Length; i++)
                {
                    DailyRewardItem item = rewardData.RewardItems[i];
                    slots[i] = new DailyRewardSlotSnapshot(i, SaveData.currentProgress > i, SaveData.currentProgress >= i, item.isAdsReward);
                }

                return new DailyRewardSnapshot(new RewardDayKey(SaveData.rewardDayKey), SaveData.currentProgress, slots, GetFreeCooldownRemaining());
            }
        }

        public int GetProgress => SaveData.currentProgress;
        public int GetLastFreeClaimTime => SaveData.lastFreeClaimTime;
        public bool CanClaimFree => GetFreeCooldownRemaining() <= TimeSpan.Zero;

        private void Start()
        {
            if (!Hung.Base.ServiceSlotLifecycle.CanInitialize<IDailyRewardService>(this, this)) return;

            DontDestroyOnLoad(gameObject);
            ServiceSlots.Register<IDailyRewardService>(this);
        }

        public void ConfigureTimeRewardIntegrity(
            IClock clock,
            RewardDayPolicy dayPolicy,
            IRewardClaimCoordinator rewardCoordinator,
            string profileScope,
            DailyRewardDataSO rewardData = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.dayPolicy = dayPolicy;
            this.rewardCoordinator = rewardCoordinator;
            this.profileScope = string.IsNullOrWhiteSpace(profileScope) ? "local-profile" : profileScope;
            if (rewardData != null)
                this.rewardData = rewardData;
        }

        public void Reconcile()
        {
            DailyRewardSaveData data = saveData ??= Database.Load<DailyRewardSaveData>();
            RewardDayKey currentDay = dayPolicy.Resolve(clock.UtcNow);
            EnsureRewardDayState(data, currentDay);
            if (data.rewardDayKey > currentDay.Value)
                return;
            if (data.rewardDayKey == currentDay.Value)
                return;

            data.rewardDayKey = currentDay.Value;
            data.dayOfYear = currentDay.ToUtcDate().DayOfYear;
            data.currentProgress = 0;
            Database.Save(data);
        }

        public TimeSpan GetFreeCooldownRemaining()
        {
            DailyRewardSaveData data = SaveData;
            int cooldownSeconds = rewardData == null ? DailyRewardDataSO.FREE_REWARD_COOLDOWN_SEC : rewardData.FreeRewardCooldownSeconds;
            if (data.lastFreeClaimUtcTicks <= 0)
                return TimeSpan.Zero;

            DateTime lastClaimUtc = new(data.lastFreeClaimUtcTicks, DateTimeKind.Utc);
            TimeSpan remaining = lastClaimUtc.AddSeconds(cooldownSeconds) - clock.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public RewardClaimResult ClaimFree()
        {
            if (!CanClaimFree)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "FREE_COOLDOWN");
            if (rewardData == null)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "DAILY_REWARD_CONFIG_MISSING");

            IReadOnlyList<GameData.ItemData> rewards = ToStableRewards(rewardData.FreeRewardItem);
            // Keyed by the previous claim, which only changes once Finalize saves the new cooldown. A retry after a
            // crash between grant and save reuses the id, so the coordinator dedupes it instead of granting again.
            string token = SaveData.lastFreeClaimUtcTicks.ToString();
            RewardClaimId id = RewardClaimId.Create("daily-reward", "free", SaveData.rewardDayKey.ToString(), token, "daily-reward-free", profileScope);
            RewardClaimResult result = ClaimAndFinalize(id, rewards, "daily-reward:free:" + SaveData.rewardDayKey + ":" + token, () =>
            {
                DateTime now = clock.UtcNow;
                SaveData.lastFreeClaimUtcTicks = now.Ticks;
                SaveData.lastFreeClaimTime = (int)now.TimeOfDay.TotalSeconds;
                Database.Save(SaveData);
                return new RewardFeatureCommitResult(true);
            });
            return result;
        }

        public RewardClaimResult ClaimSequence(int slot, RewardAuthorization? authorization = null)
        {
            Reconcile();
            if (rewardData == null)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "DAILY_REWARD_CONFIG_MISSING");
            if (slot < 0 || slot >= rewardData.TotalRewardItems)
                return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "SLOT_OUT_OF_RANGE");
            if (SaveData.currentProgress > slot)
                return new RewardClaimResult(RewardGrantOutcome.IdempotentReplay);
            if (SaveData.currentProgress < slot)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "SLOT_LOCKED");

            DailyRewardItem item = rewardData.RewardItems[slot];
            string authorizationScope = "free";
            if (item.isAdsReward)
            {
                RewardClaimResult authResult = ValidateAuthorization(authorization, out authorizationScope);
                if (!authResult.Success)
                    return authResult;
            }

            IReadOnlyList<GameData.ItemData> rewards = ToStableRewards(item);
            RewardClaimId id = RewardClaimId.Create("daily-reward", "sequence", SaveData.rewardDayKey.ToString(), slot.ToString(), authorizationScope, profileScope);
            string fingerprint = "daily-reward:sequence:" + SaveData.rewardDayKey + ":" + slot + ":" + authorizationScope;
            return ClaimAndFinalize(id, rewards, fingerprint, () =>
            {
                SaveData.currentProgress = Math.Max(SaveData.currentProgress, slot + 1);
                Database.Save(SaveData);
                return new RewardFeatureCommitResult(true);
            });
        }

        private RewardClaimResult ValidateAuthorization(RewardAuthorization? authorization, out string authorizationScope)
        {
            authorizationScope = null;
            if (!authorization.HasValue)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "AUTHORIZATION_REQUIRED");

            RewardAuthorization value = authorization.Value;
            if (value.Placement != Placement.DAILY_REWARD)
                return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "AUTHORIZATION_PLACEMENT_MISMATCH");
            if (string.IsNullOrWhiteSpace(value.ProviderEvidence))
                return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "AUTHORIZATION_EVIDENCE_MISSING");
            if (value.CompletedUtc > clock.UtcNow.AddMinutes(5))
                return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "AUTHORIZATION_FROM_FUTURE");

            authorizationScope = value.RequestId;
            return new RewardClaimResult(RewardGrantOutcome.Success);
        }

        private RewardClaimResult ClaimAndFinalize(RewardClaimId id, IReadOnlyList<GameData.ItemData> rewards, string fingerprint, Func<RewardFeatureCommitResult> persistFeatureState)
        {
            if (rewardCoordinator == null)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "REWARD_COORDINATOR_MISSING");

            RewardClaimResult claim = rewardCoordinator.Claim(new RewardClaimRequest(id, "daily-reward", ToGrantItems(rewards), fingerprint));
            if (!claim.Success)
                return claim;

            return rewardCoordinator.Finalize(id, persistFeatureState);
        }

        private static void EnsureRewardDayState(DailyRewardSaveData data, RewardDayKey fallbackDay)
        {
            if (data.rewardDayKey == 0)
            {
                if (data.dayOfYear > 0 && data.dayOfYear == fallbackDay.ToUtcDate().DayOfYear)
                    data.rewardDayKey = fallbackDay.Value;
                else
                    data.rewardDayKey = fallbackDay.Value;
            }

            data.dayOfYear = new RewardDayKey(data.rewardDayKey).ToUtcDate().DayOfYear;
        }

        private static IReadOnlyList<GameData.ItemData> ToStableRewards(DailyRewardItem model)
        {
            var rewards = new List<GameData.ItemData>();
            AddExpanded(rewards, model.Type, model.value);
            return rewards;
        }

        private static void AddExpanded(List<GameData.ItemData> rewards, ItemId type, int quantity)
        {
            if (type == BaseItemIds.RefreshClearBooster)
            {
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.KnifeBooster, Quantity = quantity });
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.HintBooster, Quantity = quantity });
            }
            else if (type == BaseItemIds.AllBooster)
            {
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.ResetBooster, Quantity = quantity });
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.KnifeBooster, Quantity = quantity });
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.HintBooster, Quantity = quantity });
            }
            else
            {
                rewards.Add(new GameData.ItemData { ItemId = type, Quantity = quantity });
            }
        }

        private static IReadOnlyList<RewardGrantItem> ToGrantItems(IReadOnlyList<GameData.ItemData> rewards)
        {
            var items = new List<RewardGrantItem>();
            foreach (GameData.ItemData reward in rewards)
                items.Add(new RewardGrantItem(reward.ItemId, reward.Quantity));
            return items;
        }
    }
}

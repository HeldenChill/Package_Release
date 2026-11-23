using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.DesignPattern;
using UnityEngine;
using Hung.Data;

namespace Hung.LiveOps.SpinWheel
{
    public class SpinWheelManager : Singleton<SpinWheelManager>, ISpinWheelService
    {
        private void OnDestroy() => ServiceSlots.Unregister<ISpinWheelService>(this);

        [SerializeField] private SpinWheelDataSO wheelData;

        private IClock clock = new SystemClock();
        private RewardDayPolicy dayPolicy = new(0);
        private IRewardClaimCoordinator rewardCoordinator;
        private string profileScope = "local-profile";
        private SpinWheelSaveData saveData;

        private SpinWheelSaveData SaveData
        {
            get
            {
                if (saveData == null)
                {
                    saveData = Database.Load<SpinWheelSaveData>();
                    Reconcile();
                    Database.Save(saveData);
                }

                return saveData;
            }
        }

        public SpinAvailability Current
        {
            get
            {
                Reconcile();
                int adsLimit = wheelData == null ? 0 : wheelData.AdsSpinPerDay;
                return new SpinAvailability(!SaveData.isDoneSpinFreeToday, SaveData.adsSpinToday, adsLimit, NextResetUtc);
            }
        }

        public DateTime NextResetUtc => dayPolicy.GetNextBoundaryUtc(clock.UtcNow);
        public bool IsDoneSpinFreeToday => SaveData.isDoneSpinFreeToday;
        public int AdsSpinToday => SaveData.adsSpinToday;
        public int DayOfYear => SaveData.dayOfYear;

        private void Start()
        {
            if (!Hung.Base.ServiceSlotLifecycle.CanInitialize<ISpinWheelService>(this, this)) return;

            DontDestroyOnLoad(gameObject);
            ServiceSlots.Register<ISpinWheelService>(this);
        }

        public void ConfigureTimeRewardIntegrity(
            IClock clock,
            RewardDayPolicy dayPolicy,
            IRewardClaimCoordinator rewardCoordinator,
            string profileScope,
            SpinWheelDataSO wheelData = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.dayPolicy = dayPolicy;
            this.rewardCoordinator = rewardCoordinator;
            this.profileScope = string.IsNullOrWhiteSpace(profileScope) ? "local-profile" : profileScope;
            if (wheelData != null)
                this.wheelData = wheelData;
        }

        public SpinStartResult PrepareFreeSpin(int selectedIndex)
        {
            Reconcile();
            if (wheelData == null)
                return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "SPIN_CONFIG_MISSING");
            if (SaveData.isDoneSpinFreeToday)
                return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "FREE_SPIN_USED");
            SpinItem item = wheelData.FreeItem(selectedIndex);
            if (item == null)
                return new SpinStartResult(RewardGrantOutcome.InvalidReward, diagnosticCode: "SPIN_INDEX_OUT_OF_RANGE");
            return PrepareSpin(selectedIndex, false, item, "free");
        }

        public SpinStartResult PrepareAuthorizedSpin(int selectedIndex, RewardAuthorization authorization)
        {
            Reconcile();
            if (wheelData == null)
                return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "SPIN_CONFIG_MISSING");
            if (!SaveData.isDoneSpinFreeToday || SaveData.adsSpinToday >= wheelData.AdsSpinPerDay)
                return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "ADS_SPIN_UNAVAILABLE");
            if (authorization.Placement != Placement.SPIN || string.IsNullOrWhiteSpace(authorization.ProviderEvidence))
                return new SpinStartResult(RewardGrantOutcome.InvalidReward, diagnosticCode: "AUTHORIZATION_INVALID");
            if (authorization.CompletedUtc > clock.UtcNow.AddMinutes(5))
                return new SpinStartResult(RewardGrantOutcome.InvalidReward, diagnosticCode: "AUTHORIZATION_FROM_FUTURE");
            SpinItem item = wheelData.AdsItem(selectedIndex);
            if (item == null)
                return new SpinStartResult(RewardGrantOutcome.InvalidReward, diagnosticCode: "SPIN_INDEX_OUT_OF_RANGE");
            return PrepareSpin(selectedIndex, true, item, authorization.RequestId);
        }

        public RewardClaimResult CompleteSpin(string spinId)
        {
            Reconcile();
            if (string.IsNullOrWhiteSpace(spinId) || !string.Equals(spinId, SaveData.pendingSpinId, StringComparison.Ordinal))
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "SPIN_NOT_PREPARED");
            if (wheelData == null)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "SPIN_CONFIG_MISSING");

            SpinItem item = SaveData.pendingIsAds ? wheelData.AdsItem(SaveData.pendingSelectedIndex) : wheelData.FreeItem(SaveData.pendingSelectedIndex);
            if (item == null)
                return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "SPIN_INDEX_OUT_OF_RANGE");

            RewardClaimId id = new(spinId);
            IReadOnlyList<GameData.ItemData> rewards = ToStableRewards(item);
            RewardClaimResult claim = rewardCoordinator?.Claim(new RewardClaimRequest(id, "spin-wheel", ToGrantItems(rewards), SaveData.pendingPayloadFingerprint))
                                     ?? new RewardClaimResult(RewardGrantOutcome.Unavailable, "REWARD_COORDINATOR_MISSING");
            if (!claim.Success)
                return claim;

            return rewardCoordinator.Finalize(id, () =>
            {
                SpinWheelSaveData data = SaveData;
                int adsSpinToday = data.adsSpinToday, spinOrdinal = data.spinOrdinal;
                bool doneFree = data.isDoneSpinFreeToday;
                string pendingId = data.pendingSpinId, pendingFingerprint = data.pendingPayloadFingerprint;
                if (data.pendingIsAds)
                    data.adsSpinToday++;
                else
                    data.isDoneSpinFreeToday = true;
                data.spinOrdinal++;
                data.pendingSpinId = null;
                data.pendingPayloadFingerprint = null;
                try { Database.Save(data); }
                catch (Exception ex)
                {
                    // Restore so a retry finalizes the same pending spin (BUG-261114-c587).
                    data.adsSpinToday = adsSpinToday; data.spinOrdinal = spinOrdinal; data.isDoneSpinFreeToday = doneFree;
                    data.pendingSpinId = pendingId; data.pendingPayloadFingerprint = pendingFingerprint;
                    return new RewardFeatureCommitResult(false, ex.Message);
                }
                return new RewardFeatureCommitResult(true);
            });
        }

        private SpinStartResult PrepareSpin(int selectedIndex, bool isAds, SpinItem item, string authorizationScope)
        {
            string mode = isAds ? "ads" : "free";
            int ordinal = SaveData.spinOrdinal + 1;
            string fingerprint = "spin-wheel:" + SaveData.rewardDayKey + ":" + mode + ":" + selectedIndex + ":" + item.Type.Value + ":" + item.value + ":" + authorizationScope;
            RewardClaimId id = RewardClaimId.Create("spin-wheel", mode, SaveData.rewardDayKey.ToString(), ordinal.ToString(), authorizationScope, profileScope);
            SaveData.pendingSpinId = id.Value;
            SaveData.pendingSelectedIndex = selectedIndex;
            SaveData.pendingIsAds = isAds;
            SaveData.pendingPayloadFingerprint = fingerprint;
            Database.Save(SaveData);
            return new SpinStartResult(RewardGrantOutcome.Success, id.Value, selectedIndex);
        }

        private void Reconcile()
        {
            SpinWheelSaveData data = saveData ??= Database.Load<SpinWheelSaveData>();
            RewardDayKey currentDay = dayPolicy.Resolve(clock.UtcNow);
            if (data.rewardDayKey == 0)
                data.rewardDayKey = currentDay.Value;
            data.dayOfYear = new RewardDayKey(data.rewardDayKey).ToUtcDate().DayOfYear;
            if (data.rewardDayKey > currentDay.Value || data.rewardDayKey == currentDay.Value)
                return;

            data.rewardDayKey = currentDay.Value;
            data.dayOfYear = currentDay.ToUtcDate().DayOfYear;
            data.isDoneSpinFreeToday = false;
            data.adsSpinToday = 0;
            data.pendingSpinId = null;
            data.pendingPayloadFingerprint = null;
            Database.Save(data);
        }

        private static IReadOnlyList<GameData.ItemData> ToStableRewards(SpinItem item)
        {
            var rewards = new List<GameData.ItemData>();
            if (item.Type == BaseItemIds.AllBooster)
            {
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.ResetBooster, Quantity = item.value });
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.KnifeBooster, Quantity = item.value });
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.HintBooster, Quantity = item.value });
            }
            else if (item.Type == BaseItemIds.RefreshClearBooster)
            {
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.KnifeBooster, Quantity = item.value });
                rewards.Add(new GameData.ItemData { ItemId = BaseItemIds.HintBooster, Quantity = item.value });
            }
            else
            {
                rewards.Add(new GameData.ItemData { ItemId = item.Type, Quantity = item.value });
            }

            return rewards;
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

using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Base.Init;
using Hung.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Hung.Data;

namespace Hung.UI
{
    public class DailyRewardPopup : BasePopup
    {
        [FoldoutGroup("Data")]
        [SerializeField]
        private DailyRewardDataSO dailyRewardData;

        [FoldoutGroup("References")]
        [SerializeField]
        private DailyRwComponent[] dailyRwComponents;
        [FoldoutGroup("References")]
        [SerializeField]
        private DailyFreeRwItem freeRwComponent;

        [FoldoutGroup("DEBUG")]
        [SerializeField]
        private GameObject debugPanel;
        [FoldoutGroup("DEBUG")]
        [SerializeField]
        private Button increaseProgressButton;
        [FoldoutGroup("DEBUG")]
        [SerializeField]
        private Button resetProgressButton;

        #region Event Function

        private void Awake()
        {
            for (int i = 0; i < dailyRwComponents.Length; i++)
            {
                DailyRwComponent component = dailyRwComponents[i];
                component.item._OnClaimed += OnSequenceRewardClaimed;
            }
            freeRwComponent.OnClaimed += OnFreeRewardClaimed;
            if (DebugManager.Ins)
            {
                debugPanel.SetActive(true);
                increaseProgressButton.onClick.AddListener(IncreaseProgress);
                resetProgressButton.onClick.AddListener(ResetProgress);
            }

        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < dailyRwComponents.Length; i++)
            {
                var component = dailyRwComponents[i];
                component.item._OnClaimed -= OnSequenceRewardClaimed;
            }
            freeRwComponent.OnClaimed -= OnFreeRewardClaimed;
            if (DebugManager.Ins)
            {
                increaseProgressButton.onClick.RemoveListener(IncreaseProgress);
                resetProgressButton.onClick.RemoveListener(ResetProgress);
            }

            base.OnDestroy();
        }

        #endregion

        protected override void OnOpen(object param)
        {
            base.OnOpen(param);
            SetUpDailyReward();
        }

        protected void OnSequenceRewardClaimed(int slot, RewardAuthorization? authorization)
        {
            RewardClaimResult result = Locator.DailyReward.ClaimSequence(slot, authorization);
            if (!result.Success) return;

            Locator.Items?.ShowUI(true);
            SetUpDailyReward();
        }

        protected void OnFreeRewardClaimed()
        {
            RewardClaimResult result = Locator.DailyReward.ClaimFree();
            if (!result.Success) return;

            Locator.Items?.ShowUI(true);
            SetUpDailyReward();
        }
        private void SetUpDailyReward()
        {
            DailyRewardSnapshot snapshot = Locator.DailyReward.Current;
            freeRwComponent.SetUp(dailyRewardData.FreeRewardItem, snapshot.CanClaimFree ? DailyFreeRwItem.State.CanClaim : DailyFreeRwItem.State.Cooldown);
            for (int i = 0; i < dailyRwComponents.Length; i++)
            {
                DailyRwComponent component = dailyRwComponents[i];
                DailyRewardItem rwItem = dailyRewardData.RewardItems[i];
                component.item.SetUp(i, rwItem, GetState(i, rwItem));
                component.SetHighlight(component.item.CurrentState == DailyRwItem.State.Claimed);
            }
            // UIManager.Ins.GetUI<HomeCanvas>()?.SetUpFeatureBtn(FeatureButtonType.DailyReward);
            UIManager.Ins.UpdateAllUI();
            return;

            DailyRwItem.State GetState(int index, DailyRewardItem rwItem)
            {
                int progress = snapshot.Progress;
                if (progress < index) return DailyRwItem.State.Lock;
                if (progress > index) return DailyRwItem.State.Claimed;
                return rwItem.isAdsReward ? DailyRwItem.State.ClaimAds : DailyRwItem.State.ClaimFree;
            }
        }

        [Serializable]
        private class DailyRwComponent
        {
            public DailyRwItem item;
            public GameObject highlight;

            public void SetHighlight(bool isHighlight)
            {
                highlight.SetActive(isHighlight);
            }
        }
        #region TESTING

        [FoldoutGroup("TESTING")]
        [Button]
        private void IncreaseProgress()
        {
            SetUpDailyReward();
        }

        [FoldoutGroup("TESTING")]
        [Button]
        private void ResetProgress()
        {
            SetUpDailyReward();
        }
        #endregion

    }


}

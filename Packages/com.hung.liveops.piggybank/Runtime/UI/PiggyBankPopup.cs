using System;
using Hung.Base;
using Hung.Base.Init;
using Hung.LiveOps.PiggyBank;
using Hung.UI;
using Hung.Common;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

// NOTE: THIS Class is derived from UISCanvas, so it should not be packed when packing feature content
using Hung.DesignPattern;
using Hung.Data;

namespace Hung.UI
{
    public class PiggyBankPopup : BasePopup
    {
        #region Properties

        [FoldoutGroup("Button")] [SerializeField]
        private Button btnClaim;

        [FoldoutGroup("Pig Control")] [SerializeField]
        private PiggyBankDataSO piggyData;
        [FoldoutGroup("Pig Control")] [SerializeField]
        private TextMeshProUGUI txtValue;
        [FoldoutGroup("Pig Control")] [SerializeField]
        private Slider sliderProgress;
        [FoldoutGroup("Pig Control")] [SerializeField]
        private TextMeshProUGUI txtProgress;
        [FoldoutGroup("Pig Control")] [SerializeField]
        private TextMeshProUGUI txtPrice;
        [FoldoutGroup("Pig Control")] [SerializeField]
        private GameObject lockClaimIcon;
        
        #endregion

        #region DEBUG
        
        [FoldoutGroup("DEBUG")] [SerializeField]
        private GameObject debugPanel;
        [FoldoutGroup("DEBUG")] [SerializeField]
        private Button increaseProgressButton;
        [FoldoutGroup("DEBUG")] [SerializeField]
        private Button resetProgressButton;

        #endregion

        private void Awake()
        {
            if (DebugManager.Ins)
            {
                debugPanel.SetActive(true);
                increaseProgressButton.onClick.AddListener(TryAddProgress);
                resetProgressButton.onClick.AddListener(TryResetProgress);
            }
        }

        protected override void Start()
        {
            base.Start();
            btnClaim.onClick.AddListener(OnClaim);
        }

        protected override void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClaim);
            if (DebugManager.Ins)
            {
                increaseProgressButton.onClick.RemoveListener(TryAddProgress);
                resetProgressButton.onClick.RemoveListener(TryResetProgress);
            }
            base.OnDestroy();
        }

        protected override void OnOpen(object param)
        {
            base.OnOpen(param);
            SetupPiggy();
        }
        
        private void OnClaim()
        {
            Debug.Log("Claim");
            BreakPiggy();
        }

        private void SetupPiggy()
        {
            int currentGold = Locator.PiggyBank.PiggyGoldProgress;
            int targetGold = piggyData.GoldCollected;
            if (currentGold > targetGold) currentGold = targetGold;
            txtValue.SetText(targetGold.ToString());
            sliderProgress.maxValue = targetGold;
            sliderProgress.value = currentGold;
            txtProgress.SetText($"{currentGold}/{targetGold}");
            bool isLock = currentGold < targetGold;
            btnClaim.interactable = !isLock;
            lockClaimIcon.SetActive(isLock);
            txtPrice.SetText(piggyData.LocalizedCostToBreak());
        }

        private void BreakPiggy()
        {
            if (lockClaimIcon.activeSelf) return;
            DevLog.Log(DevId.Hung, "Claim GOLD: " + piggyData.GoldCollected);
            btnClaim.interactable = false; // one purchase per tap
            // ponytail: gold is granted in the legacy purchase callback, after the store order is Completed.
            // A kill before this callback still loses the gold; closing that needs an IPurchaseGrantHandler
            // for the piggy product in the game's purchase composition.
            Locator.IAPService.Purchase(IAP_ITEM.PIGGY_BANK, OnPiggyBankPurchased, OnPiggyBankPurchaseFailed);
        }

        private void OnPiggyBankPurchaseFailed()
        {
            if (this != null) SetupPiggy();
        }

        private void OnPiggyBankPurchased()
        {
            int gold = piggyData.GoldCollected;
            // Paid gold is granted and saved first; progress reset and UI come after, so neither a failed
            // reset save nor a popup destroyed during the purchase can lose it.
            Locator.Items.ClaimItem(BaseItemIds.Gold, gold);
            Database.Save(Locator.Data.GetData<GameData>(), GameData.SaveKey);
            Locator.PiggyBank.ResetPiggyProgress();
            EventBus<PiggyBankRewardClaimedEvent>.Raise(new PiggyBankRewardClaimedEvent { goldAmount = gold });
            if (this == null) return;
            SetupPiggy();
            Close();
        }

        #region TEST

        [FoldoutGroup("TESTING")] [Button]
        private void TryAddProgress()
        {
            Locator.PiggyBank.IncreasePiggyProgress();
            SetupPiggy();
            // UIManager.Ins.GetUI<HomeCanvas>()?.SetUpFeatureBtn(FeatureButtonType.PiggyBank);
        }
        
        [FoldoutGroup("TESTING")] [Button]
        private void TryResetProgress()
        {
            Locator.PiggyBank.ResetPiggyProgress();
            SetupPiggy();
            // UIManager.Ins.GetUI<HomeCanvas>()?.SetUpFeatureBtn(FeatureButtonType.PiggyBank);
        }

        #endregion
    }
}

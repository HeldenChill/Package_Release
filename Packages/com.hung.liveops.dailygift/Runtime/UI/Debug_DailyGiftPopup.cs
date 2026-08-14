using UnityEngine;

namespace Hung.UI
{
    using Hung.Base;
    using Hung.UI;
    using TMPro;
    using UnityEngine.UI;

    public class Debug_DailyGiftPopup : UICanvas
    {
        public readonly Color ENABLE_COLOR = Color.white;
        public readonly Color DISABLE_COLOR = new Color(1, 1, 1, 0);

        [SerializeField]
        Button activeBtn;
        [SerializeField]
        Image activeBtnImage;
        [SerializeField]
        Button addUnlockDayBtn;
        [SerializeField]
        Button clearAllDayBtn;
        [SerializeField]
        GameObject contentRegion;

        bool isActive;
        GameData gameData;
        protected GameData GameData => gameData ??= Locator.Data.GetData<GameData>();
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                contentRegion.SetActive(value);
                if (isActive)
                {
                    activeBtnImage.color = ENABLE_COLOR;
                }
                else
                {
                    activeBtnImage.color = DISABLE_COLOR;
                }
            }
        }
        protected void Awake()
        {
            IsActive = contentRegion.activeInHierarchy;
            activeBtn.onClick.AddListener(OnActiveBtnClick);
            addUnlockDayBtn.onClick.AddListener(OnUnlockNextDayClick);
            clearAllDayBtn.onClick.AddListener(OnClearAllDayClick);
        }

        protected void OnDestroy()
        {
            activeBtn.onClick.RemoveListener(OnActiveBtnClick);
            addUnlockDayBtn.onClick.RemoveListener(OnUnlockNextDayClick);
            clearAllDayBtn.onClick.RemoveListener(OnClearAllDayClick);
        }

        protected void OnActiveBtnClick()
        {
            IsActive = !IsActive;
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
        }
        protected void OnUnlockNextDayClick()
        {
            Locator.DailyGift.NextDay();
            UIManager.Ins.UpdateAllUI();
        }
        protected void OnClearAllDayClick()
        {
            Locator.DailyGift.ResetData();
            UIManager.Ins.UpdateAllUI();
        }
    }
}

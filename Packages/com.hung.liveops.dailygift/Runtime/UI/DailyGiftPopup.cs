using System.Collections.Generic;
using UnityEngine;

namespace Hung.UI
{
    using Hung.UI;
    using Hung.Base.Init;
    using Hung.LiveOps.DailyGift;
    using Hung.Base;

    public class DailyGiftPopup : BasePopup
    {
        public List<DailyGiftUIItem> listDailyGiftUI;
        [SerializeField]
        protected ScriptableObject dailyGiftData;

        private Hung.Data.LiveOps.IDailyGiftConfig Config => dailyGiftData as Hung.Data.LiveOps.IDailyGiftConfig
            ?? throw new System.InvalidOperationException(
                $"{nameof(DailyGiftPopup)} requires a ScriptableObject implementing {nameof(Hung.Data.LiveOps.IDailyGiftConfig)}.");
        protected void Awake()
        {
            for (int i = 0; i < listDailyGiftUI.Count; i++)
            {
                listDailyGiftUI[i]._OnItemClick += OnItemClick;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < listDailyGiftUI.Count; i++)
            {
                listDailyGiftUI[i]._OnItemClick -= OnItemClick;
            }
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            foreach (Hung.Data.LiveOps.IDailyGiftDay day in Config.DailyGifts)
            {
                if (day.Day < 0 || day.Day >= listDailyGiftUI.Count)
                    throw new System.InvalidOperationException($"Daily gift day {day.Day} has no UI slot.");
                listDailyGiftUI[day.Day].SetConfig(day);
            }
        }
        protected override void OnOpen(object param)
        {
            base.OnOpen(param);
            // DropRwItemController.Ins.ShowUI(false);
            if (DebugManager.Ins != null)
            {
                UIManager.Ins.OpenUI<Debug_DailyGiftPopup>();
            }
        }

        public override void Hide()
        {
            base.Hide();
            // DropRwItemController.Ins.ShowUI(false);
            if (DebugManager.Ins != null)
            {
                UIManager.Ins.CloseUI<Debug_DailyGiftPopup>();
            }
        }

        protected void OnItemClick(DailyGiftUIItem item)
        {
            Locator.Items?.SetSpawnPosition(item.transform);  
            Locator.DailyGift.ClaimDailyGift(item.Config.Day);
            UIManager.Ins.UpdateAllUI();
        }
    }
}

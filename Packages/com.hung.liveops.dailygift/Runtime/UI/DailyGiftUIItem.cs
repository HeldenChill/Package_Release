using System;
using System.Collections.Generic;
using Hung.UI;
using Hung.Base;
using Hung.Data.LiveOps;
// using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using Utilities;

namespace Hung.LiveOps.DailyGift
{
    public class DailyGiftUIItem : MonoBehaviour
    {
        public event Action<DailyGiftUIItem> _OnItemClick;
        private IDailyGiftDay _config;
        public TextMeshProUGUI txtDay;
        public List<UIItem> listRwItemUI;
        public GameObject frameActive, frameClaimed;
        [SerializeField]
        UIButton uiButton;
        public IDailyGiftDay Config => _config;
        protected void Awake()
        {
            uiButton._OnClick += ClaimReward;
        }
        protected void OnDestroy()
        {
            uiButton._OnClick -= ClaimReward;
        }
        public void SetConfig(IDailyGiftDay config)
        {
            _config = config;
            txtDay.SetText($"Day {_config.Day + 1}");
            for (var i = 0; i < listRwItemUI.Count; i++)
            {
                if (i < _config.Rewards.Count)
                {
                    listRwItemUI[i].gameObject.SetActive(true);
                    listRwItemUI[i].SetData(_config.Rewards[i].Quantity, false);
                    ItemId item = _config.Rewards[i].ItemId;
                    if (!item.IsValid)
                        throw new InvalidOperationException("Daily gift reward is missing ItemId. Run ItemId asset migration.");
                    listRwItemUI[i].SetData(Locator.Items.GetPresentation(item).Icon);
                }
                else
                {
                    listRwItemUI[i].gameObject.SetActive(false);
                }
            }

            frameClaimed.SetActive(Locator.DailyGift.DataModel.listDailyGiftStatus[_config.Day]);
            frameActive.SetActive(!frameClaimed.activeSelf && ((Locator.DailyGift.DataModel.dayCount - 1) % 7) >= _config.Day);
        }

        public void ClaimReward(int code)
        {
            if (!frameActive.activeSelf) return;
            Locator.Audio?.PlaySfx(SFX_TYPE.ITEM_CLAIM);
            // AudioManager.Ins.PlaySound(AudioId.UnlockItem);
            Locator.Items?.SetSpawnPosition(frameActive.transform);
            _OnItemClick?.Invoke(this);
            frameActive.SetActive(false);
            frameClaimed.SetActive(true);
        }
    }
}

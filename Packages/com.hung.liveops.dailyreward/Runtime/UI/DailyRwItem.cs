using System;
using Hung.UI;
using System.Collections.Generic;
using Hung.Base;
using Hung.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Hung.UI
{
    public class DailyRwItem : MonoBehaviour
    {
        public enum State
        {
            Lock = 0,
            ClaimFree = 1,
            ClaimAds = 2,
            Claimed = 3
        }

        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text quantity;
        [SerializeField] private UIButton claimBtn;
        [SerializeField] private GameObject adsIcon;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject claimedIcon;

        private List<GameData.ItemData> _currentRewardItems;
        private readonly IClock authorizationClock = new SystemClock();

        public State CurrentState { get; private set; }

        public int SlotIndex { get; private set; }

        public event Action<int, RewardAuthorization?> _OnClaimed;

        private void Awake()
        {
            claimBtn._OnClick += OnClaimButtonClick;
        }

        private void OnDestroy()
        {
            claimBtn._OnClick -= OnClaimButtonClick;
        }

        public void SetUp(int slotIndex, DailyRewardItem rwItem, State state)
        {
            SlotIndex = slotIndex;
            ItemPresentation itemConfig = Locator.Items.GetPresentation(rwItem.Type);
            _currentRewardItems = GetRewardItem(rwItem);
            if (rewardIcon != null) rewardIcon.sprite = itemConfig.Icon;
            if (quantity != null) quantity.SetText(rwItem.value.ToString());
            SetState(state);
        }

        private void OnClaimButtonClick(int index)
        {
            if (CurrentState != State.ClaimFree && CurrentState != State.ClaimAds) return;
            if (CurrentState is State.ClaimFree)
            {
                ClaimItem(null);
            }
            else if (CurrentState is State.ClaimAds)
            {
                if (Locator.Ads == null)
                {
                    DevLog.Log(DevId.Hung, $"ERROR: Ads is null, cannot claim with ads");
                    return;
                }
                else
                {
                    string requestId = Guid.NewGuid().ToString("N");
                    Locator.Ads.Reward?.Show(
                        () => ClaimItem(new RewardAuthorization(requestId, Placement.DAILY_REWARD, authorizationClock.UtcNow, "ads-callback")),
                        null,
                        Placement.DAILY_REWARD);
                }
            }

            return;

            void ClaimItem(RewardAuthorization? authorization)
            {
                Locator.Items?.SetSpawnPosition(rewardIcon.transform);
                _OnClaimed?.Invoke(SlotIndex, authorization);
                Locator.Audio?.PlaySfx(BaseAudioIds.ItemClaim);
                SetState(State.Claimed);
                
            }
        }

        private void SetState(State state)
        {
            claimBtn.SetInteractable(state is State.ClaimFree or State.ClaimAds);
            lockIcon.SetActive(state == State.Lock);
            adsIcon.SetActive(state == State.ClaimAds);
            claimedIcon.SetActive(state == State.Claimed);
            CurrentState = state;
        }

        private List<GameData.ItemData> GetRewardItem(DailyRewardItem model)
        {
            List<GameData.ItemData> listRwItem = new List<GameData.ItemData>();
            ItemId type = model.Type;
            if (type != BaseItemIds.AllBooster && type != BaseItemIds.RefreshClearBooster)
            {
                listRwItem.Add(new GameData.ItemData()
                {
                    ItemId = type,
                    Quantity = model.value
                });
            }
            else if (type == BaseItemIds.RefreshClearBooster)
            {
                listRwItem.Add(new GameData.ItemData()
                {
                    ItemId = BaseItemIds.KnifeBooster,
                    Quantity = model.value
                });
                listRwItem.Add(new GameData.ItemData()
                {
                    ItemId = BaseItemIds.HintBooster,
                    Quantity = model.value
                });
            }
            else if (type == BaseItemIds.AllBooster)
            {
                listRwItem.Add(new GameData.ItemData()
                {
                    ItemId = BaseItemIds.ResetBooster,
                    Quantity = model.value
                });
                listRwItem.Add(new GameData.ItemData()
                {
                    ItemId = BaseItemIds.KnifeBooster,
                    Quantity = model.value
                });
                listRwItem.Add(new GameData.ItemData()
                {
                    ItemId = BaseItemIds.HintBooster,
                    Quantity = model.value
                });
            }

            return listRwItem;
        }
    }
}

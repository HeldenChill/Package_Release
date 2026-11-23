using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Data;
using Hung.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Hung.UI
{
    public class DailyFreeRwItem : MonoBehaviour
    {
        public enum State
        {
            CanClaim = 0,
            Cooldown = 1,
        }

        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text quantity;
        [SerializeField] private UIButton claimBtn;
        [SerializeField] private GameObject cooldownIcon;
        [SerializeField] private TMP_Text stateText;

        private List<GameData.ItemData> _currentRewardItems;

        private State CurrentState { get; set; }

        public event Action OnClaimed;
        private int _lastClaimTime;
        private const string CLAIM_TEXT = "Claim";
        private const string COOLDOWN_TEXT = "59:59";

        private event Action OnCanClaim;

        private void Awake()
        {
            claimBtn._OnClick += OnClaimButtonClick;
            // OnCanClaim = () => UIManager.Ins.GetUI<HomeCanvas>()?.SetUpFeatureBtn(FeatureButtonType.DailyReward);
        }

        private void Update()
        {
            if (CurrentState is State.CanClaim) return;
            // set cooldown time as format mm:ss
            int timeRemain = Mathf.Max(0, (int)Locator.DailyReward.GetFreeCooldownRemaining().TotalSeconds);
            stateText.text = GetCooldownTime(timeRemain);
            if (timeRemain == 0 && Locator.DailyReward.CanClaimFree)
            {
                SetState(State.CanClaim);
            }
        }

        private static string GetCooldownTime(int timeRemain)
        {
            int min = timeRemain / 60;
            int sec = timeRemain % 60;
            return $"{min:00}:{sec:00}";
        }

        private void OnDestroy()
        {
            claimBtn._OnClick -= OnClaimButtonClick;
            OnCanClaim = null;
            OnClaimed = null;
        }

        public void SetUp(DailyRewardItem rwItem, State state)
        {
            ItemPresentation itemConfig = Locator.Items.GetPresentation(rwItem.Type);
            _currentRewardItems = GetRewardItem(rwItem);
            if (rewardIcon != null) rewardIcon.sprite = itemConfig.Icon;
            if (quantity != null) quantity.SetText(rwItem.value.ToString());
            SetState(state);
        }

        private void OnClaimButtonClick(int index)
        {
            if (CurrentState is State.Cooldown) return;
            ClaimItem();
            UIManager.Ins.UpdateAllUI();
            return;

            void ClaimItem()
            {
                Locator.Items?.SetSpawnPosition(rewardIcon.transform);
                OnClaimed?.Invoke();
                // H3Vibration.Haptic(HapticTypes.SoftImpact);
                Locator.Audio?.PlaySfx(BaseAudioIds.ItemClaim);
                // AudioManager.Ins.PlaySound(AudioId.UnlockItem);
                SetState(State.Cooldown);
            }
        }

        private void SetState(State state)
        {
            bool isCanClaim = state is State.CanClaim;
            claimBtn.SetInteractable(isCanClaim);
            cooldownIcon.SetActive(!isCanClaim);
            stateText.text = isCanClaim ? CLAIM_TEXT : COOLDOWN_TEXT;
            if (!isCanClaim)
            {
                _lastClaimTime = Locator.DailyReward.GetLastFreeClaimTime;
            }
            else
            {
                OnCanClaim?.Invoke();
            }
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

        public static State GetSaveState => Locator.DailyReward.CanClaimFree ? State.CanClaim : State.Cooldown;
    }
}

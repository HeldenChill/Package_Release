using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Base.Init;
using Hung.UI;
using Hung.Common;
using DG.Tweening;
// using MoreMountains.NiceVibrations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;
using Hung.Data;

namespace Hung.UI
{
    public class SpinWheelPopup : BasePopup
    {
        [FoldoutGroup("References")] [SerializeField]
        private List<UIItem> listRwItems;
        [FoldoutGroup("References")] [SerializeField]
        private TextMeshProUGUI spinStateText;
        [FoldoutGroup("References")] [SerializeField]
        private UIButton spinButton;
        [FoldoutGroup("References")] [SerializeField]
        private GameObject spinAdsIcon;
        [FoldoutGroup("References")] [SerializeField]
        private GameObject spinLockIcon;
        [FoldoutGroup("References")] [SerializeField]
        private GameObject freeSpinContainer;
        [FoldoutGroup("References")] [SerializeField]
        private TextMeshProUGUI freeSpinCountDownText;
        [FoldoutGroup("References")] [SerializeField]
        private Transform wheel;
        
        [SerializeField] private SpinWheelDataSO wheelData;
        
        [FoldoutGroup("DEBUG")] [SerializeField]
        private Button resetButton;
        
        private bool _isSpinning;
        private TimeSpan _timeUntilNextDay;
        private readonly IClock uiClock = new SystemClock();
        
        private void Awake()
        {
            spinButton._OnClick += OnSpinButtonClicked;
            if (DebugManager.Ins)
            {
                resetButton.gameObject.SetActive(true);
                resetButton.onClick.AddListener(ResetWheelData);
            }
        }

        private void Update()
        {
            if (!freeSpinContainer.gameObject.activeSelf) return;
            _timeUntilNextDay = Locator.SpinWheel.NextResetUtc - uiClock.UtcNow;
            
            // if the time is up, reset the spin state
            if (_timeUntilNextDay.TotalSeconds <= 0)
            {
                FetchSpinData();
                return;
            }

            // Update the countdown display
            freeSpinCountDownText.SetText("Next free spin in " + _timeUntilNextDay.ToString(@"hh\:mm\:ss"));
        }

        protected override void OnDestroy()
        {
            spinButton._OnClick -= OnSpinButtonClicked;
            if (DebugManager.Ins)
            {
                resetButton.onClick.RemoveListener(ResetWheelData);
            }
            base.OnDestroy();
        }

        protected override void OnOpen(object param)
        {
            base.OnOpen(param);
            _isSpinning = false;
            FetchSpinData();
        }

        public override void UpdateUI()
        {
            base.UpdateUI();
            FetchSpinData();
        }

        private const string DAILY_FREE_SPIN = "Daily Free";
        
        private void FetchSpinData()
        {
            SpinAvailability availability = Locator.SpinWheel.Current;
            bool isDoneFreeSpin = !availability.FreeAvailable;
            SetUpSpinItem();
            SetupSpinButton();
            
            // Calculate initial time until midnight
            _timeUntilNextDay = availability.NextResetUtc - uiClock.UtcNow;
            
            return;

            void SetUpSpinItem()
            {
                List<SpinItem> spinItems = isDoneFreeSpin ? wheelData.SpinAdsItems : wheelData.SpinFreeItems; 
                for (int i = 0; i < listRwItems.Count; i++)
                {
                    listRwItems[i].SetData(Locator.Items.GetPresentation(spinItems[i].Type).Icon);
                    listRwItems[i].SetData(spinItems[i].value);
                }
            }

            void SetupSpinButton()
            {
                if (!isDoneFreeSpin)
                {
                    spinAdsIcon.SetActive(false);
                    spinLockIcon.SetActive(false);
                    spinButton.SetInteractable(true);
                    spinStateText.SetText(DAILY_FREE_SPIN);
                    freeSpinContainer.gameObject.SetActive(false);
                } else {
                    freeSpinContainer.gameObject.SetActive(true);
                    int adsSpinToday = availability.AdsSpinToday;
                    int adsSpinPerDay = availability.AdsSpinLimit;
                    spinStateText.SetText($"{adsSpinToday}/{adsSpinPerDay}");
                    if (adsSpinToday < adsSpinPerDay)
                    {
                        spinAdsIcon.SetActive(true);
                        spinLockIcon.SetActive(false);
                        spinButton.SetInteractable(true);
                    }
                    else
                    {
                        spinAdsIcon.SetActive(false);
                        spinLockIcon.SetActive(true);
                        spinButton.SetInteractable(false);
                    }
                }
            }
        }

        private void OnSpinButtonClicked(int index)
        {
            if (_isSpinning) return;
            bool isFreeItem = !Locator.SpinWheel.IsDoneSpinFreeToday;
            // H3Vibration.Haptic(HapticTypes.Selection);
            if (isFreeItem)
            {
                Spin();
            }
            else
            {
                if (Locator.Ads == null || Locator.Ads.Reward == null) return;
                string requestId = Guid.NewGuid().ToString("N");
                Locator.Ads.Reward.Show(
                    () => Spin(new RewardAuthorization(requestId, Placement.SPIN, uiClock.UtcNow, "ads-callback")),
                    null,
                    Placement.SPIN);
            }
        }

        private void Spin(RewardAuthorization? authorization = null)
        {
            bool isFreeItem = !Locator.SpinWheel.IsDoneSpinFreeToday;
            int spinIndex = isFreeItem ? wheelData.GetRandomFreeItemIndex() : wheelData.GetRandomAdsItemIndex();
            SpinStartResult start = isFreeItem
                ? Locator.SpinWheel.PrepareFreeSpin(spinIndex)
                : Locator.SpinWheel.PrepareAuthorizedSpin(spinIndex, authorization.Value);
            if (!start.Success) return;

            _isSpinning = true;
            spinButton.SetInteractable(false);
            spinLockIcon.SetActive(true);
            closeButton.interactable = false;
            LockBgCloseInteractable(true);
            spinStateText.SetText("Spinning...");
            Locator.Audio?.PlaySfx(BaseAudioIds.SpinWheel);
            wheel.transform.DOLocalRotate(new Vector3(0, 0, 3600f + 360f * ((float)spinIndex / listRwItems.Count)), 6f, RotateMode.FastBeyond360)
                .OnComplete(
                    () =>
                    {
                        Locator.Items.SetSpawnPosition(listRwItems[spinIndex].Icon.transform);
                        // H3Vibration.Haptic(HapticTypes.Success);
                        Locator.Audio?.PlaySfx(BaseAudioIds.SpinEnd);
                        RewardClaimResult result = Locator.SpinWheel.CompleteSpin(start.SpinId);
                        if (result.Success)
                            Locator.Items?.ShowUI(true);
                        // UIManager.Ins.GetUI<HomeCanvas>()?.SetUpFeatureBtn(FeatureButtonType.SpinWheel);
                        UIManager.Ins.UpdateAllUI();
                        OnSpinDone();
                    });
        }

        private void OnSpinDone()
        {
            _isSpinning = false;
            spinButton.SetInteractable(true);
            closeButton.interactable = true;
            spinLockIcon.SetActive(false);
            LockBgCloseInteractable(false);
            FetchSpinData();
        }
        #region TEST

        [Button]
        private void ResetWheelData()
        {
            FetchSpinData();
            UIManager.Ins.UpdateAllUI();
        }

        #endregion
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.IronSource
{
    using System;
    using Hung.Base;
    using Unity.Services.LevelPlay;
    using Hung.Ads;

    public class RewardedAds : MonoBehaviour, IRewardedAdsProvider
    {
        // These ad units are configured to always serve test ads.
        public event Action OnAdsDisplayFail;
        public event Action OnAdsLoadFail;
        public event Action OnAdsReceiveReward;
        public event Action OnAdsHidden;
        public event Action OnAdsLoaded;
#if UNITY_ANDROID
        private string _adUnitId = "7q99lf1v9khkzscv";
#elif UNITY_IOS
        private string _adUnitId = "dn76st3mcjy0ptre";
#else
        private string _adUnitId = "unused";
#endif
        Placement placement;
        private LevelPlayRewardedAd rewardedAd;
        public bool IsLoading { get; protected set; } = false;
        public bool IsCanShow => rewardedAd != null && rewardedAd.IsAdReady();
        public void Show()
        {
            if (IsCanShow)
            {
                rewardedAd.ShowAd();
            }
            else
            {
                Load();
            }
        }

        public void Show(Placement placement = Placement.NONE)
        {
            this.placement = placement;
            Locator.Analytics.AdsRewardOffer(placement);
            Show();
        }
        /// <summary>
        /// Loads the rewarded ad.
        /// </summary>
        public void Load()
        {
            // Clean up the old ad before loading a new one.
            if (IsLoading)
            {
                Debug.LogWarning("Reward Iron Source Reward Ad is already loading.");
                return;
            }
            
            if (rewardedAd == null)
            {
                rewardedAd ??= new LevelPlayRewardedAd(_adUnitId);
                RegisterEventHandlers(rewardedAd);
            }

            IsLoading = true;
            Debug.Log("Loading the iron source rewarded ad.");

            // send the request to load the ad.
            rewardedAd.LoadAd();
        }
        private void RegisterEventHandlers(LevelPlayRewardedAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdLoaded += OnAdLoaded;
            ad.OnAdLoadFailed += OnAdLoadFailed;
            ad.OnAdDisplayed += OnAdsDisplay;
            ad.OnAdDisplayFailed += HandleAdDisplayFail;
            ad.OnAdRewarded += OnAdPaidHandler;
            ad.OnAdClosed += OnAdClosed;
            ad.OnAdClicked += OnAdClick;
            ad.OnAdInfoChanged += OnAdInfoChanged;
        }

        private void UnregisterEventHandlers(LevelPlayRewardedAd ad)
        {
            ad.OnAdLoaded -= OnAdLoaded;
            ad.OnAdLoadFailed -= OnAdLoadFailed;
            ad.OnAdDisplayed -= OnAdsDisplay;
            ad.OnAdDisplayFailed -= HandleAdDisplayFail;
            ad.OnAdRewarded -= OnAdPaidHandler;
            ad.OnAdClosed -= OnAdClosed;
            ad.OnAdClicked -= OnAdClick;
            ad.OnAdInfoChanged -= OnAdInfoChanged;
        }
        private void OnAdPaidHandler(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            // Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
            //         adValue.Value,
            //         adValue.CurrencyCode));
            Debug.Log(String.Format(info.AdUnitId, reward.Name, reward.Amount));
            OnAdsReceiveReward?.Invoke();
            Locator.Analytics.AdsRewardComplete(placement, "IRON SOURCE");
        }
        private void OnAdLoaded(LevelPlayAdInfo info)
        {
            IsLoading = false;
            OnAdsLoaded?.Invoke();
            Locator.Analytics.GoogleFireBaseTrackEvent("af_rewarded_successfullyloaded");
            Locator.Analytics.AdsRewardLoad();
        }
        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogError("Rewarded ad failed to load an ad " +
                                       "with error : " + error);
            IsLoading = false;
            OnAdsLoadFail?.Invoke();
            Locator.Analytics.AdsRewardLoadFail();
        }
        private void OnAdsDisplay(LevelPlayAdInfo info)
        {
            Locator.Analytics.GoogleFireBaseTrackEvent("af_rewarded_displayed");
            Locator.Analytics.AdsRewardShow(placement);
        }
        private void HandleAdDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Locator.Analytics.AdsRewardShowFail(placement, error.ToString());
            OnAdsDisplayFail?.Invoke();
        }

        private void OnAdInfoChanged(LevelPlayAdInfo info)
        {
            
        }

        private void OnAdClick(LevelPlayAdInfo info)
        {
            Debug.Log("Rewarded ad was clicked.");
            Locator.Analytics.AdsRewardClick(placement);
        }

        private void OnAdClosed(LevelPlayAdInfo info)
        {
            OnAdsHidden?.Invoke();
            Debug.Log("Rewarded ad full screen content closed.");
        }

    }


}
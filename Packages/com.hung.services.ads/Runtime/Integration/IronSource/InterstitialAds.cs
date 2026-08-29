using UnityEngine;

namespace Hung.Ads.Integration.IronSource
{
    using global::Utilities;
    using System;
    using Hung.Base;
    using Unity.Services.LevelPlay;
    using Hung.Ads;

    public class InterstitialAds : MonoBehaviour, IInterstitialAdsProvider
    {
        public event Action OnAdsDisplayFail;
        public event Action OnAdsLoadFail;
        public event Action OnAdsDone;
        public event Action OnAdsLoaded;
        // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
        private string _adUnitId = "p74uqor5srgglawl";
#elif UNITY_IPHONE
        private string _adUnitId = "meq3kot9alewr14u";
#else
        private string _adUnitId = "unused";
#endif
        private LevelPlayInterstitialAd interestialAd;
        public bool IsCanShow => interestialAd != null && interestialAd.IsAdReady();
        public bool IsLoading { get; protected set; } = false;
        Placement placement;
        public void Show(Placement placement)
        {
            this.placement = placement;
            if (IsCanShow)
            {
                interestialAd.ShowAd();
            }
            else
            {
                Load();
            }
        }
        /// <summary>
        /// Loads the rewarded ad.
        /// </summary>
        public void Load()
        {
            // Clean up the old ad before loading a new one.
            if (IsLoading)
            {
                Debug.LogWarning("Reward Iron Source Inter Ad is already loading.");
                return;
            }

            if (interestialAd == null)
            {
                interestialAd ??= new LevelPlayInterstitialAd(_adUnitId);
                RegisterEventHandlers(interestialAd);
            }
            IsLoading = true;
            Debug.Log("Loading the iron source rewarded ad.");

            // send the request to load the ad.
            interestialAd.LoadAd();
        }
        private void RegisterEventHandlers(LevelPlayInterstitialAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdLoaded += OnAdLoaded;
            ad.OnAdLoadFailed += OnAdLoadFailed;
            ad.OnAdDisplayed += OnAdsDisplay;
            ad.OnAdDisplayFailed += HandleAdDisplayFail;
            ad.OnAdClosed += OnAdClosed;
            ad.OnAdClicked += OnAdClick;
            ad.OnAdInfoChanged += OnAdInfoChanged;
        }

        private void UnregisterEventHandlers(LevelPlayInterstitialAd ad)
        {
            ad.OnAdLoaded -= OnAdLoaded;
            ad.OnAdLoadFailed -= OnAdLoadFailed;
            ad.OnAdDisplayed -= OnAdsDisplay;
            ad.OnAdDisplayFailed -= HandleAdDisplayFail;
            ad.OnAdClosed -= OnAdClosed;
            ad.OnAdClicked -= OnAdClick;
            ad.OnAdInfoChanged -= OnAdInfoChanged;
        }
        private void OnAdLoaded(LevelPlayAdInfo info)
        {
            Debug.Log("Interstitial ad load complete.");
            IsLoading = false;
            OnAdsLoaded?.Invoke();
            Locator.Analytics.GoogleFireBaseTrackEvent("af_inters_successfullyloaded");
            Locator.Analytics.AdsInterLoadComplete();
        }
        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            IsLoading = false;
            Debug.LogError("interstitial ad failed to load an ad " +
                                       "with error : " + error);
            OnAdsLoadFail?.Invoke();
            Locator.Analytics.AdsInterFail(error.ToString());
        }
        private void OnAdsDisplay(LevelPlayAdInfo info)
        {
            Locator.Analytics.GoogleFireBaseTrackEvent("af_inters_displayed");
            Locator.Analytics.AdsInterShow(placement);
            Debug.Log("Interstitial ad full screen content opened.");
        }
        private void HandleAdDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogError("Interstitial ad failed to open full screen content with error: " + error);
            Locator.Analytics.AdsInterFail(error.ToString());
            OnAdsDisplayFail?.Invoke();
        }

        private void OnAdInfoChanged(LevelPlayAdInfo info)
        {
            
        }

        private void OnAdClick(LevelPlayAdInfo info)
        {
            Debug.Log("Interstitial ad was clicked.");
            Locator.Analytics.AdsInterClick();
        }

        private void OnAdClosed(LevelPlayAdInfo info)
        {
            OnAdsDone?.Invoke();
            Locator.Analytics.AdsInterComplete();
            Debug.Log("Interstitial ad full screen content closed.");
        }
    }
}
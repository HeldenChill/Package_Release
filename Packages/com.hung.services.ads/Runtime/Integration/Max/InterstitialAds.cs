using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    using Hung.Base;
    using global::Utilities;
    using Hung.Ads;
    public class InterstitialAds : MonoBehaviour, IInterstitialAdsProvider
    {
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsDone;
        public event Action OnAdsLoaded;
#if UNITY_ANDROID
        protected string adUnitId = "32125378dbdd09ec";
#elif UNITY_IOS
        protected string adUnitId = "c9e81c552c2aea2c";
#else
        protected string adUnitId = "unused";
#endif
        public bool IsCanShow => MaxSdk.IsInterstitialReady(adUnitId);
        public bool IsLoading { get; protected set; } = false;
        Placement placement;

        // Start is called before the first frame update
        void Start()
        {
            // Attach callback
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
            // Load the first interstitial
        }

        private void OnDestroy()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialAdFailedToDisplayEvent;
        }
        public virtual void Load()
        {
            Debug.Log("[MAX]: Load Interestial Ads");
            IsLoading = true;   
            Locator.Analytics.AdsInterLoad();
            MaxSdk.LoadInterstitial(adUnitId);
        }

        public virtual void Show(Placement placement)
        {
            this.placement = placement;
            Locator.Analytics.GoogleFireBaseTrackEvent("af_inters_logicgame");
            if (IsCanShow)
            {
                MaxSdk.ShowInterstitial(adUnitId);
                // AnalysticManager.Ins.AdsInterShow(Placement.IN_GAME);
                DevLog.Log(DevId.Hung, "ADS: SHOWING INTER");
            }
            else
            {
                Debug.LogWarning("Interstitial ad is not ready. Make sure to load it before showing.");
            }
        }

        protected virtual void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is ready for you to show. MaxSdk.IsInterstitialReady(adUnitId) now returns 'true'
            IsLoading = false;
            Locator.Analytics.GoogleFireBaseTrackEvent("af_inters_successfullyloaded");
            Locator.Analytics.AdsInterLoadComplete();
            OnAdsLoaded?.Invoke();
            Debug.Log($"[MAX]: OnInterstitialLoadedEvent");

        }

        protected virtual void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Interstitial ad failed to load 
            // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)
            IsLoading = false;
            Locator.Analytics.AdsInterFail(errorInfo.AdLoadFailureInfo);
            OnAdsLoadFail?.Invoke();
            Debug.Log($"[MAX]: OnInterstitialLoadFailedEvent");

        }

        protected virtual void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Locator.Analytics.GoogleFireBaseTrackEvent("af_inters_displayed");
            Locator.Analytics.AdsInterShow(placement);
            Debug.Log($"[MAX]: OnInterstitialDisplayedEvent");

        }

        protected virtual void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad failed to display. AppLovin recommends that you load the next ad.
            Locator.Analytics.AdsInterFail(errorInfo.AdLoadFailureInfo);
            OnAdsDisplayFail?.Invoke();
            Debug.Log($"[MAX]: OnInterstitialAdFailedToDisplayEvent");
        }

        protected virtual void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnInterstitialClickedEvent");
            Locator.Analytics.AdsInterClick();
        }

        protected virtual void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is hidden. Pre-load the next ad.
            DevLog.Log(DevId.Hung, "ADS: HIDE INTER");
            Debug.Log($"[MAX]: OnInterstitialHiddenEvent");
            OnAdsDone?.Invoke();
            Locator.Analytics.AdsInterComplete();
        }
    }
}

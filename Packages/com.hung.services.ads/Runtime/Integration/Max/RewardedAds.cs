using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    using Hung.Base;
    using Hung.Ads;
    public class RewardedAds : MonoBehaviour, IRewardedAdsProvider
    {
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsReceiveReward;
        public event Action OnAdsHidden;
        public event Action OnAdsLoaded;
#if UNITY_ANDROID
        protected string adUnitId = "432785b62168b940";
#elif UNITY_IOS
        protected string adUnitId = "52936bd01ff5f441";
#else
        protected string adUnitId = "unused";
#endif
        protected int retryAttempt;
        protected Placement placement;

        public bool IsCanShow => MaxSdk.IsRewardedAdReady(adUnitId);
        public bool IsLoading { get; protected set; } = false;
        // Start is called before the first frame update
        void Start()
        {
            // Attach callback
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        }
        public void Load()
        {
            if (IsLoading)
            {
                Debug.LogWarning("Reward Max Reward Ad is already loading.");
                return;
            }
            MaxSdk.LoadRewardedAd(adUnitId);
            IsLoading = true;
        }

        public void Show(Placement placement = Placement.NONE)
        {
            this.placement = placement;
            Locator.Analytics.AdsRewardOffer(placement);
            Show();
        }

        protected void Show()
        {
            Locator.Analytics.GoogleFireBaseTrackEvent("af_rewarded_logicgame");
            if (IsCanShow)
            {
                MaxSdk.ShowRewardedAd(adUnitId);
            }
            else
            {
                Debug.LogWarning("Rewarded ad is not ready. Make sure to load it before showing.");
            }
        }

        protected void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is ready for you to show. MaxSdk.IsRewardedAdReady(adUnitId) now returns 'true'.\
            IsLoading = false;
            Locator.Analytics.GoogleFireBaseTrackEvent("af_rewarded_successfullyloaded");
            Debug.Log($"[MAX]: OnRewardedAdLoadedEvent");
            Locator.Analytics.AdsRewardLoadComplete();
            OnAdsLoaded?.Invoke();
            // Reset retry attempt
            retryAttempt = 0;
        }

        protected void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Rewarded ad failed to load
            // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).
            IsLoading = false;
            Debug.Log($"[MAX]: OnRewardedAdLoadFailedEvent");
            retryAttempt++;
            Locator.Analytics.AdsRewardLoadFail();
            OnAdsLoadFail?.Invoke();
        }

        protected void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnRewardedAdDisplayedEvent");
            Locator.Analytics.AdsRewardShow(placement);
            Locator.Analytics.GoogleFireBaseTrackEvent("af_rewarded_displayed");
        }

        protected void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad failed to display. AppLovin recommends that you load the next ad.
            Debug.Log($"[MAX]: OnRewardedAdFailedToDisplayEvent");
            Locator.Analytics.AdsRewardShowFail(placement, errorInfo.AdLoadFailureInfo);
            OnAdsDisplayFail?.Invoke();
        }

        protected void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnRewardedAdClickedEvent");
            Locator.Analytics.AdsRewardClick(placement);
        }

        protected void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is hidden. Pre-load the next ad
            Debug.Log($"[MAX]: OnRewardedAdHiddenEvent");
            OnAdsHidden?.Invoke();
        }

        protected void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnRewardedAdReceivedRewardEvent");
            Locator.Analytics.AdsRewardComplete(placement, "MAX");
            OnAdsReceiveReward?.Invoke();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    using Hung.Ads;

    public class BannerAds : MonoBehaviour, IBannerAdsProvider
    {
        public event Action OnAdsLoadFail;
        public event Action OnAdsLoaded;
#if UNITY_ANDROID
        protected string bannerAdUnitId = "a7c437edac263459";
#elif UNITY_IOS
        protected string bannerAdUnitId = "55abdd76de507c69";
#else
        protected string bannerAdUnitId = "unused";
#endif
        // Start is called before the first frame update
        public void InitBanner()
        {
            MaxSdk.CreateBanner(bannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

            MaxSdk.CreateBanner(bannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.StartBannerAutoRefresh(bannerAdUnitId);
        }
        public virtual void Show()
        {
            MaxSdk.ShowBanner(bannerAdUnitId);
        }

        public virtual void Hide()
        {
            MaxSdk.HideBanner(bannerAdUnitId);
        }
        public virtual void Destroy()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent -= OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent -= OnBannerAdCollapsedEvent;
            MaxSdk.DestroyBanner(bannerAdUnitId);
        }
        public virtual void Load()
        {

        }

        protected virtual void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnBannerAdLoadedEvent");
            OnAdsLoaded?.Invoke();
        }

        protected virtual void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log($"[MAX]: OnBannerAdLoadFailedEvent");
            OnAdsLoadFail?.Invoke();
        }

        protected virtual void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnBannerAdClickedEvent");
        }

        protected virtual void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnBannerAdRevenuePaidEvent");
        }

        protected virtual void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnBannerAdExpandedEvent");
        }

        protected virtual void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"[MAX]: OnBannerAdCollapsedEvent");
        }
    }
}

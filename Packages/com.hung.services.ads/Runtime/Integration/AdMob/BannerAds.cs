using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Hung.Ads.Integration.AdMob
{
    using Hung.Ads;

    /// <summary>AdMob bottom anchored banner (Google Mobile Ads 9.x).</summary>
    public sealed class AdMobBanner : MonoBehaviour, IBannerAdsProvider
    {
        string adUnitId;
        BannerView view;

        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;

        /// <summary>Ad unit id for this platform.</summary>
        public void SetAdUnitId(string id) => adUnitId = id;

        public void InitBanner()
        {
            view?.Destroy();
            view = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
            view.OnBannerAdLoaded += () => OnAdsLoaded?.Invoke();
            view.OnBannerAdLoadFailed += error =>
            {
                Debug.LogWarning("[AdMob] Banner load failed: " + error);
                OnAdsLoadFail?.Invoke();
            };
        }

        public void Load()
        {
            if (!AdMobAdsInstaller.Initialized)
            {
                if (waitingForInit) return;
                waitingForInit = true;
                AdMobAdsInstaller.WhenInitialized(() =>
                {
                    if (this == null) return;
                    if (waitingForInit) { waitingForInit = false; Load(); }
                });
                StartCoroutine(FailIfInitStalls());
                return;
            }
            if (view == null) InitBanner();
            view.LoadAd(new AdRequest());
        }

        public void Show() => view?.Show();
        public void Hide() => view?.Hide();

        public void Destroy()
        {
            view?.Destroy();
            view = null;
        }

        bool waitingForInit;

        System.Collections.IEnumerator FailIfInitStalls()
        {
            yield return new WaitForSecondsRealtime(AdMobAdsInstaller.InitWaitTimeoutSeconds);
            if (!waitingForInit) yield break;
            waitingForInit = false;
            Debug.LogWarning("[AdMob] Banner load abandoned: SDK init did not finish in time.");
            OnAdsLoadFail?.Invoke();
        }

        void OnDestroy() => Destroy();
    }
}

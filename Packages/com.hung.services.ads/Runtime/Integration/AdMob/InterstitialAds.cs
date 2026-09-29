using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Hung.Ads.Integration.AdMob
{
    using Hung.Base;
    using Hung.Ads;

    /// <summary>AdMob interstitial provider (Google Mobile Ads 9.x).</summary>
    public sealed class AdMobInterstitial : MonoBehaviour, IInterstitialAdsProvider
    {
        string adUnitId;
        InterstitialAd ad;

        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsDone;

        public bool IsCanShow => ad != null && ad.CanShowAd();
        public bool IsLoading { get; private set; }

        /// <summary>Ad unit id for this platform.</summary>
        public void SetAdUnitId(string id) => adUnitId = id;

        public void Load()
        {
            if (IsLoading) return;
            if (!AdMobAdsInstaller.Initialized)
            {
                IsLoading = true;
                AdMobAdsInstaller.WhenInitialized(() =>
                {
                    if (this == null) return;
                    if (waitingForInit) { waitingForInit = false; IsLoading = false; Load(); }
                });
                waitingForInit = true;
                StartCoroutine(FailIfInitStalls());
                return;
            }
            ad?.Destroy();
            ad = null;
            IsLoading = true;
            InterstitialAd.Load(adUnitId, new AdRequest(), (loaded, error) =>
            {
                IsLoading = false;
                if (error != null || loaded == null)
                {
                    Debug.LogWarning("[AdMob] Interstitial load failed: " + error);
                    OnAdsLoadFail?.Invoke();
                    return;
                }
                ad = loaded;
                ad.OnAdFullScreenContentClosed += () => OnAdsDone?.Invoke();
                ad.OnAdFullScreenContentFailed += e =>
                {
                    Debug.LogWarning("[AdMob] Interstitial display failed: " + e);
                    OnAdsDisplayFail?.Invoke();
                };
                OnAdsLoaded?.Invoke();
            });
        }

        public void Show(Placement placement)
        {
            if (!IsCanShow)
            {
                OnAdsDisplayFail?.Invoke();
                return;
            }
            ad.Show();
        }

        bool waitingForInit;

        System.Collections.IEnumerator FailIfInitStalls()
        {
            yield return new WaitForSecondsRealtime(AdMobAdsInstaller.InitWaitTimeoutSeconds);
            if (!waitingForInit) yield break;
            waitingForInit = false;
            IsLoading = false;
            Debug.LogWarning("[AdMob] Interstitial load abandoned: SDK init did not finish in time.");
            OnAdsLoadFail?.Invoke();
        }

        void OnDestroy() => ad?.Destroy();
    }
}

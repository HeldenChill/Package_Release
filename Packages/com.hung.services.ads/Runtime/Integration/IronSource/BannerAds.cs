using System;
using UnityEngine;

namespace Hung.Ads.Integration.IronSource
{
    using Unity.Services.LevelPlay;
    using global::Utilities;
    using Hung.Ads;
    public class BannerAds : MonoBehaviour, IBannerAdsProvider
    {
        public event Action OnAdsLoadFail;
        public event Action OnAdsLoaded;
#if UNITY_ANDROID
        string adUnitId = "caist7kheol69eb9";
#elif UNITY_IOS
        string adUnitId = "13412gjfsybcw6vi";
#else
        string adUnitId = "unused";
#endif
        private bool isAdInit = false;
        // Start is called before the first frame update
        LevelPlayBannerAd bannerAds;
        bool isBannerPrepared = false;
        bool isAdReady = false;

        public void Show()
        {
            if (isAdReady)
            {
                bannerAds.ShowAd();
            }
            else
            {
                Load();
            }

        }
        public void Hide()
        {
            bannerAds.HideAd();
        }
        public void Destroy()
        {
            if (bannerAds != null)
            {
                bannerAds.OnAdLoaded -= BannerOnAdLoadedEvent;
                bannerAds.OnAdLoadFailed -= BannerOnAdLoadFailedEvent;
                bannerAds.OnAdDisplayed -= BannerOnAdDisplayedEvent;
                bannerAds.OnAdDisplayFailed -= BannerOnAdDisplayFailedEvent;
                bannerAds.OnAdClicked -= BannerOnAdClickedEvent;
                bannerAds.OnAdCollapsed -= BannerOnAdCollapsedEvent;
                bannerAds.OnAdLeftApplication -= BannerOnAdLeftApplicationEvent;
                bannerAds.OnAdExpanded -= BannerOnAdExpandedEvent;
                bannerAds.DestroyAd();
                bannerAds = null;
            }
        }
        public void InitBanner()
        {
            if (bannerAds == null)
            {
                bannerAds = new LevelPlayBannerAd(adUnitId);
            }
            isBannerPrepared = true;
            isAdInit = true;
        }
        public void Load()
        {
            DevLog.Log(DevId.Hung, "Show Ironsource banner");
            isAdReady = false;
            if (isAdInit)
            {
                if (!isBannerPrepared)
                {
                    if (bannerAds == null)
                        InitBanner();
                }

                bannerAds.LoadAd();
                isBannerPrepared = false;
            }
        }

        public void RegisterEventHandlers(LevelPlayBannerAd bannerAd)
        {
            bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
            bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
            bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
            bannerAd.OnAdClicked += BannerOnAdClickedEvent;
            bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
            bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
            bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;
        }

        private void BannerOnAdExpandedEvent(LevelPlayAdInfo info)
        {
            DevLog.Log(DevId.Hung, "Banner view full screen content opened.");
        }

        private void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo info)
        {

        }

        private void BannerOnAdCollapsedEvent(LevelPlayAdInfo info)
        {
            DevLog.Log(DevId.Hung, "Banner view full screen content closed.");
        }

        private void BannerOnAdClickedEvent(LevelPlayAdInfo info)
        {
            DevLog.Log(DevId.Hung, "Banner view was clicked.");
        }

        private void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo info, LevelPlayAdError error)
        {

        }

        private void BannerOnAdDisplayedEvent(LevelPlayAdInfo info)
        {

        }

        private void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Load();
            OnAdsLoadFail?.Invoke();
            DevLog.Log(DevId.Hung, "Banner view failed to load an ad with error : "
                + error);
        }

        private void BannerOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            DevLog.Log(DevId.Hung, "Banner view loaded an ad with response : "
                                + info);
            isAdReady = true;
            Show();
            OnAdsLoaded?.Invoke();
        }
    }
}
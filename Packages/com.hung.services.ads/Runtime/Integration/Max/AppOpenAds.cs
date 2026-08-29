using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    public class AppOpenAds : MonoBehaviour
    {
        protected string adUnitId;

        private void Start()
        {
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenDismissedEvent;
        }

        public void OnAppOpenDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Load();
        }

        public void Load()
        {
            MaxSdk.LoadAppOpenAd(adUnitId);
        }

        public void Show()
        {
            if (MaxSdk.IsAppOpenAdReady(adUnitId))
            {
                MaxSdk.ShowAppOpenAd(adUnitId);
            }
            else
            {
                Load();
            }
        }
    }
}
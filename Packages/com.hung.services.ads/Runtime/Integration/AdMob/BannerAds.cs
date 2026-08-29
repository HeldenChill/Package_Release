using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.AdMob
{
    // using Utilities;
    // public class BannerAds : MonoBehaviour
    // {
    //     string collapsibleBannerAdUnitId = "ca-app-pub-2150063081064097/9136677502";
    //     bool isBannerOpen = false;
    //     private bool isAdmobInited = false;
    //     // Start is called before the first frame update
    //     BannerView bannerView;
    //     bool isBannerPrepared = false;
    //     public bool IsBannerOpen => isBannerOpen;

    //     public void Show()
    //     {
    //         Load();
    //     }
    //     public void Hide()
    //     {
    //         if (isBannerOpen)
    //         {
    //             isBannerOpen = false;
    //             if (bannerView != null)
    //             {
    //                 DevLog.Log(DevId.Hung, "Banner Hide");
    //                 bannerView.Hide();
    //             }
    //         }
    //     }
    //     public void InitBanner()
    //     {
    //         bannerView = new BannerView(collapsibleBannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
    //         isBannerPrepared = true;

    //         bannerView.OnBannerAdLoaded += () =>
    //         {
    //             DevLog.Log(DevId.Hung, "Banner view loaded an ad with response : "
    //                 + bannerView.GetResponseInfo());
    //         };
    //         bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
    //         {
    //             Load();
    //             DevLog.Log(DevId.Hung, "Banner view failed to load an ad with error : "
    //                 + error);
    //         };
    //         bannerView.OnAdPaid += (AdValue adValue) =>
    //         {
    //             DevLog.Log(DevId.Hung, String.Format("Banner view paid {0} {1}.",
    //                 adValue.Value,
    //                 adValue.CurrencyCode));
    //         };
    //         bannerView.OnAdImpressionRecorded += () =>
    //         {
    //             DevLog.Log(DevId.Hung, "Banner view recorded an impression.");
    //         };
    //         bannerView.OnAdClicked += () =>
    //         {
    //             DevLog.Log(DevId.Hung, "Banner view was clicked.");
    //         };
    //         bannerView.OnAdFullScreenContentOpened += () =>
    //         {
    //             DevLog.Log(DevId.Hung, "Banner view full screen content opened.");
    //         };
    //         bannerView.OnAdFullScreenContentClosed += () =>
    //         {
    //             DevLog.Log(DevId.Hung, "Banner view full screen content closed.");
    //         };
    //         isAdmobInited = true;
    //     }
    //     public void Load()
    //     {
    //         DevLog.Log(DevId.Hung, "Show banner collap");
    //         if (isAdmobInited)
    //         {
    //             if (!isBannerPrepared)
    //             {
    //                 if (bannerView != null)
    //                     DestroyBannerView();
    //                 InitBanner();
    //             }

    //             var adRequest = new AdRequest();
    //             // Create an extra parameter that aligns the bottom of the expanded ad to the
    //             // bottom of the bannerView.
    //             adRequest.Extras.Add("collapsible", "bottom");
    //             adRequest.Extras.Add("collapsible_request_id", RandomIDForBannerCollap());
    //             bannerView.LoadAd(adRequest);
    //             isBannerPrepared = false;
    //         }
    //     }
    //     private string RandomIDForBannerCollap()
    //     {
    //         int count = 5;
    //         string result = "";
    //         for (int i = 0; i < count; i++)
    //         {
    //             for (int j = 0; j < count; j++)
    //             {
    //                 int rdValue = UnityEngine.Random.Range(0, 10);
    //                 result += rdValue.ToString();
    //             }
    //             if (i < 4)
    //                 result += "-";
    //         }
    //         return result;
    //     }
    //     private void DestroyBannerView()
    //     {
    //         if (bannerView != null)
    //         {
    //             DevLog.Log(DevId.Hung, "Destroying banner view.");
    //             bannerView.Destroy();
    //             bannerView = null;
    //         }
    //     }
    // }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.AdMob
{
//     using GoogleMobileAds.Api;
//     using GoogleMobileAds.Common;
//     using System;

//     public class AppOpenAds : MonoBehaviour
//     {
// #if UNITY_ANDROID
//         // Test ad unit ID: ca-app-pub-3940256099942544/3419835294
//         private const string adUnitId = "ca-app-pub-9819920607806935/6926588812";
// #elif UNITY_IOS
//     // Test ad unit ID: ca-app-pub-3940256099942544/5662855259
//     private const string adUnitId = "<YOUR_IOS_APPOPEN_AD_UNIT_ID>";
// #else
//     private const string adUnitId = "unexpected_platform";
// #endif


//         private AppOpenAd ad;

//         private bool isShowingAd = false;

//         private bool IsAdAvailable
//         {
//             get
//             {
//                 return ad != null;
//             }
//         }
//         public void Show()
//         {
//             if (ad != null && ad.CanShowAd())
//             {
//                 Debug.Log("Showing app open ad.");
//                 ad.Show();
//             }
//             else
//             {
//                 Debug.LogError("App open ad is not ready yet.");
//             }
//         }
//         public void Load()
//         {
//             // TODO: Load an app open ad.
//             // Clean up the old ad before loading a new one.
//             if (ad != null)
//             {
//                 ad.Destroy();
//                 ad = null;
//             }

//             Debug.Log("Loading the app open ad.");

//             // Create our request used to load the ad.
//             AdRequest adRequest = new AdRequest();
            

//             // send the request to load the ad.
//             AppOpenAd.Load(adUnitId, adRequest,
//                 (AppOpenAd ad, LoadAdError error) =>
//                 {
//                     // if error is not null, the load request failed.
//                     if (error != null || ad == null)
//                     {
//                         Debug.LogError("app open ad failed to load an ad " +
//                                        "with error : " + error);
//                         return;
//                     }

//                     Debug.Log("App open ad loaded with response : "
//                               + ad.GetResponseInfo());

//                     this.ad = ad;
//                     RegisterEventHandlers(ad);
//                 });

//         }
//         private void RegisterEventHandlers(AppOpenAd ad)
//         {
//             // Raised when the ad is estimated to have earned money.
//             ad.OnAdPaid += (AdValue adValue) =>
//             {
//                 Debug.Log(String.Format("App open ad paid {0} {1}.",
//                     adValue.Value,
//                     adValue.CurrencyCode));
//             };
//             // Raised when an impression is recorded for an ad.
//             ad.OnAdImpressionRecorded += () =>
//             {
//                 Debug.Log("App open ad recorded an impression.");
//             };
//             // Raised when a click is recorded for an ad.
//             ad.OnAdClicked += () =>
//             {
//                 Debug.Log("App open ad was clicked.");
//             };
//             // Raised when an ad opened full screen content.
//             ad.OnAdFullScreenContentOpened += () =>
//             {
//                 Debug.Log("App open ad full screen content opened.");
//             };
//             // Raised when the ad closed full screen content.
//             ad.OnAdFullScreenContentClosed += () =>
//             {
//                 Debug.Log("App open ad full screen content closed.");
//                 Load();
//             };
//             // Raised when the ad failed to open full screen content.
//             ad.OnAdFullScreenContentFailed += (AdError error) =>
//             {
//                 Debug.LogError("App open ad failed to open full screen content " +
//                                "with error : " + error);
//                 Load();
//             };
//         }
//         private void OnAppStateChanged(AppState state)
//         {
//             Debug.Log("App State changed to : " + state);

//             // if the app is Foregrounded and the ad is available, show it.
        
//         }
        
//     }
}
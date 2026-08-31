using UnityEngine;

namespace Hung.Ads.Integration.AdMob
{
//     using GoogleMobileAds.Api;
//     using Utilities;
//     using System;
//     using static MaxSdkBase;
//     using Base;

//     public class InterstitialAds : MonoBehaviour
//     {
//         public Action _OnAdsDisplayFail;
//         public Action<LoadAdError> _OnAdsLoadFail;
//         public Action _OnAdsDone;
//         public Action _OnAdsLoaded;
//         // These ad units are configured to always serve test ads.
// #if UNITY_ANDROID
//         private string _adUnitId = "ca-app-pub-2150063081064097/4308335731";
// #elif UNITY_IPHONE
//   private string _adUnitId = "ca-app-pub-3940256099942544/4411468910";
// #else
//   private string _adUnitId = "unused";
// #endif
//         private bool isLoading = false;
//         private InterstitialAd _interstitialAd;
//         public bool IsCanShow => _interstitialAd != null;

//         /// /// <summary>
//         /// Shows the interstitial ad.
//         /// </summary>
//         public void Show()
//         {
//             if (_interstitialAd != null && _interstitialAd.CanShowAd())
//             {
//                 Debug.Log("Showing interstitial ad.");
//                 _interstitialAd.Show();
//             }
//             else
//             {
//                 Debug.LogError("Interstitial ad is not ready yet.");
//             }
//         }
//         /// <summary>
//         /// Loads the interstitial ad.
//         /// </summary>
//         public void Load()
//         {
//             if (isLoading)
//             {
//                 Debug.LogWarning("Ad is already loading.");
//                 return;
//             }
//             isLoading = true;
//             // Clean up the old ad before loading a new one.
//             if (_interstitialAd != null)
//             {
//                 UnregisterEventHandlers(_interstitialAd);
//                 _interstitialAd.Destroy();
//                 _interstitialAd = null;
//             }

//             Debug.Log("Loading the interstitial ad.");

//             // create our request used to load the ad.
//             var adRequest = new AdRequest();

//             // send the request to load the ad.
//             AnalysticManager.Ins.AdsInterLoad();
//             InterstitialAd.Load(_adUnitId, adRequest,
//                 (InterstitialAd ad, LoadAdError error) =>
//                 {
//                     // if error is not null, the load request failed.
//                     isLoading = false;
//                     if (error != null || ad == null)
//                     {
//                         Debug.LogError("interstitial ad failed to load an ad " +
//                                        "with error : " + error);
//                         _OnAdsLoadFail?.Invoke(error);
//                         AnalysticManager.Ins.AdsInterFail(error.ToString());
//                         return;
//                     }

//                     Debug.Log("Interstitial ad loaded with response : "
//                               + ad.GetResponseInfo());

//                     _interstitialAd = ad;
//                     _OnAdsLoaded?.Invoke();
//                     AnalysticManager.Ins.GoogleFireBaseTrackEvent("af_inters_successfullyloaded");
//                     AnalysticManager.Ins.AdsInterLoadComplete();
//                     RegisterEventHandlers(_interstitialAd);
//                 });
//         }
//         // Method to detach event handlers for cleanup before ad is destroyed
//         private void UnregisterEventHandlers(InterstitialAd ad)
//         {
//             ad.OnAdPaid -= OnAdPaidHandler;
//             ad.OnAdImpressionRecorded -= OnAdImpressionHandler;
//             ad.OnAdClicked -= OnAdClickedHandler;
//             ad.OnAdFullScreenContentOpened -= OnAdOpenedHandler;
//             ad.OnAdFullScreenContentClosed -= OnAdClosedHandler;
//             ad.OnAdFullScreenContentFailed -= OnAdDisplayFailedHandler;
//         }

//         // Updated RegisterEventHandlers using separate handler methods
//         private void RegisterEventHandlers(InterstitialAd ad)
//         {
//             ad.OnAdPaid += OnAdPaidHandler;
//             ad.OnAdImpressionRecorded += OnAdImpressionHandler;
//             ad.OnAdClicked += OnAdClickedHandler;
//             ad.OnAdFullScreenContentOpened += OnAdOpenedHandler;
//             ad.OnAdFullScreenContentClosed += OnAdClosedHandler;
//             ad.OnAdFullScreenContentFailed += OnAdDisplayFailedHandler;
//         }

//         // Separate handler methods
//         private void OnAdPaidHandler(AdValue adValue)
//         {
//             Debug.Log($"Interstitial ad paid {adValue.Value} {adValue.CurrencyCode}.");
//             AnalysticManager.Ins.AdsInterComplete();
//         }

//         private void OnAdImpressionHandler()
//         {
//             Debug.Log("Interstitial ad recorded an impression.");
//         }

//         private void OnAdClickedHandler()
//         {
//             Debug.Log("Interstitial ad was clicked.");
//             AnalysticManager.Ins.AdsInterClick();
//         }

//         private void OnAdOpenedHandler()
//         {
//             AnalysticManager.Ins.GoogleFireBaseTrackEvent("af_inters_displayed");
//             AnalysticManager.Ins.AdsInterShow(Placement.IN_GAME);
//             Debug.Log("Interstitial ad full screen content opened.");
//         }

//         private void OnAdClosedHandler()
//         {
//             Debug.Log("Interstitial ad full screen content closed.");
//             _OnAdsDone?.Invoke();
//         }

//         private void OnAdDisplayFailedHandler(AdError error)
//         {
//             Debug.LogError("Interstitial ad failed to open full screen content with error: " + error);
//             AnalysticManager.Ins.AdsInterFail(error.ToString());
//             _OnAdsDisplayFail?.Invoke();
//         }
//     }
}
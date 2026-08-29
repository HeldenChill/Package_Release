using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads.Integration.AdMob
{
//     using System;
//     using Base;
//     using GoogleMobileAds.Api;

//     public class RewardedAds : MonoBehaviour
//     {
//         // These ad units are configured to always serve test ads.
//         public Action _OnAdsDisplayFail;
//         public Action<LoadAdError> _OnAdsLoadFail;
//         public Action _OnAdsReceiveReward;
//         public Action _OnAdsHidden;
//         public Action _OnAdsLoaded;
// #if UNITY_ANDROID
//         private string _adUnitId = "ca-app-pub-2150063081064097/4008473901";
// #elif UNITY_IPHONE
//   private string _adUnitId = "ca-app-pub-3940256099942544/1712485313";
// #else
//   private string _adUnitId = "unused";
// #endif

//         private RewardedAd rewardedAd;
//         private bool isLoading = false;

//         public bool IsCanShow => rewardedAd != null && rewardedAd.CanShowAd();
//         public void Show()
//         {
//             const string rewardMsg =
//                 "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

//             if (IsCanShow)
//             {
//                 rewardedAd.Show((Reward reward) =>
//                 {
//                     // TODO: Reward the user.
//                     Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
//                     _OnAdsReceiveReward?.Invoke();
//                     AnalysticManager.Ins.AdsRewardComplete(Placement.NONE, "AD MOB");
//                 });
//             }
//             else
//             {
//                 Load();
//             }
//         }
//         /// <summary>
//         /// Loads the rewarded ad.
//         /// </summary>
//         public void Load()
//         {
//             // Clean up the old ad before loading a new one.
//             if (isLoading)
//             {
//                 Debug.LogWarning("Reward Ad is already loading.");
//                 return;
//             }

//             isLoading = true;
//             if (rewardedAd != null)
//             {
//                 UnregisterEventHandlers(rewardedAd);
//                 rewardedAd.Destroy();
//                 rewardedAd = null;
//             }

//             Debug.Log("Loading the rewarded ad.");

//             // create our request used to load the ad.
//             var adRequest = new AdRequest();

//             // send the request to load the ad.
//             RewardedAd.Load(_adUnitId, adRequest,
//                 (RewardedAd ad, LoadAdError error) =>
//                 {
//                     // if error is not null, the load request failed.
//                     isLoading = false;
//                     if (error != null || ad == null)
//                     {
//                         Debug.LogError("Rewarded ad failed to load an ad " +
//                                        "with error : " + error);
//                         _OnAdsLoadFail?.Invoke(error);
//                         AnalysticManager.Ins.AdsRewardLoadFail();
//                         return;
//                     }
//                     Debug.Log("Rewarded ad loaded with response : "
//                               + ad.GetResponseInfo());

//                     rewardedAd = ad;
//                     RegisterEventHandlers(ad);
//                     _OnAdsLoaded?.Invoke();
//                     AnalysticManager.Ins.GoogleFireBaseTrackEvent("af_rewarded_successfullyloaded");
//                     AnalysticManager.Ins.AdsRewardLoad();
//                 });
//         }
//         private void RegisterEventHandlers(RewardedAd ad)
//         {
//             // Raised when the ad is estimated to have earned money.
//             ad.OnAdPaid += OnAdPaidHandler;
//             ad.OnAdImpressionRecorded += OnAdImpressionHandler;
//             ad.OnAdClicked += OnAdClickedHandler;
//             ad.OnAdFullScreenContentOpened += OnAdOpenedHandler;
//             ad.OnAdFullScreenContentClosed += OnAdClosedHandler;
//             ad.OnAdFullScreenContentFailed += OnAdFailedHandler;
//         }

//         private void UnregisterEventHandlers(RewardedAd ad)
//         {
//             ad.OnAdPaid -= OnAdPaidHandler;
//             ad.OnAdImpressionRecorded -= OnAdImpressionHandler;
//             ad.OnAdClicked -= OnAdClickedHandler;
//             ad.OnAdFullScreenContentOpened -= OnAdOpenedHandler;
//             ad.OnAdFullScreenContentClosed -= OnAdClosedHandler;
//             ad.OnAdFullScreenContentFailed -= OnAdFailedHandler;
//         }
//         private void OnAdPaidHandler(AdValue adValue)
//         {
//             Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
//                     adValue.Value,
//                     adValue.CurrencyCode));
//         }

//         private void OnAdImpressionHandler()
//         {
//             Debug.Log("Rewarded ad recorded an impression.");
//         }

//         private void OnAdClickedHandler()
//         {
//             Debug.Log("Rewarded ad was clicked.");
//             AnalysticManager.Ins.AdsRewardClick(Placement.NONE);
//         }

//         private void OnAdOpenedHandler()
//         {
//             Debug.Log("Rewarded ad full screen content opened.");
//             AnalysticManager.Ins.GoogleFireBaseTrackEvent("af_rewarded_displayed");
//             AnalysticManager.Ins.AdsRewardShow(Placement.NONE);
//         }

//         private void OnAdClosedHandler()
//         {
//             _OnAdsHidden?.Invoke();
//             Debug.Log("Rewarded ad full screen content closed.");
//         }

//         private void OnAdFailedHandler(AdError error)
//         {
//             Debug.LogError("Rewarded ad failed to open full screen content " +
//                                "with error : " + error);
//             AnalysticManager.Ins.AdsRewardShowFail(Placement.NONE, error.ToString());
//             _OnAdsDisplayFail?.Invoke();
//         }
//     }


}
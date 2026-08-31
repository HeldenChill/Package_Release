//using _Game.DesignPattern;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//public class MaxInit : Singleton<MaxInit>
//{
//    // Start is called before the first frame update
//    void Start()
//    {
//        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) => {
//            // AppLovin SDK is initialized, start loading ads
//            LoadInterstitialAd();
//            LoadRewardedAd();
//        };

//        MaxSdk.SetSdkKey("ZoNyqu_piUmpl33-qkoIfRp6MTZGW9M5xk1mb1ZIWK6FN9EBu0TXSHeprC3LMPQI7S3kTc1-x7DJGSV8S-gvFJ");
//        MaxSdk.InitializeSdk();

//        // Register rewarded ad callbacks
//        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoaded;
//        //  MaxSdkCallbacks.OnRewardedAdFailedToDisplayEvent += OnRewardedAdFailedToDisplay;
//        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayed;
//        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClicked;
//        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHidden;
//        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedReward;
//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }

//    // Function to load an interstitial ad
//    private void LoadInterstitialAd()
//    {
//        MaxSdk.LoadInterstitial("c8ea8f6273eef263");
//    }

//    // Function to load a rewarded ad
//    private void LoadRewardedAd()
//    {
//        MaxSdk.LoadRewardedAd("e9cf2be5a77927b0");
//    }

//    // Function to show an interstitial ad
//    public void ShowInterstitialAd()
//    {
//        if (MaxSdk.IsInterstitialReady("c8ea8f6273eef263"))
//        {
//            MaxSdk.ShowInterstitial("c8ea8f6273eef263");
//        }
//        else
//        {
//            Debug.LogWarning("Interstitial ad is not ready. Make sure to load it before showing.");
//            LoadInterstitialAd(); // Load a new interstitial ad
//        }
//    }

//    // Function to show a rewarded ad
//    public void ShowRewardedAd()
//    {
//        if (MaxSdk.IsRewardedAdReady("e9cf2be5a77927b0"))
//        {
//            MaxSdk.ShowRewardedAd("e9cf2be5a77927b0");
//        }
//        else
//        {
//            Debug.LogWarning("Rewarded ad is not ready. Make sure to load it before showing.");
//            LoadRewardedAd(); // Load a new rewarded ad
//        }
//    }

//    // Rewarded ad loaded callback
//    private void OnRewardedAdLoaded(string adUnitId, MaxSdkBase.AdInfo info)
//    {
//        Debug.Log("Rewarded ad loaded: " + adUnitId);
//    }

//    // Rewarded ad failed to display callback
//    private void OnRewardedAdFailedToDisplay(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
//    {
//        Debug.Log("Rewarded ad failed to display: " + adUnitId + ", Error: " + errorInfo.Message);
//    }

//    // Rewarded ad displayed callback
//    private void OnRewardedAdDisplayed(string adUnitId, MaxSdkBase.AdInfo info)
//    {
//        Debug.Log("Rewarded ad displayed: " + adUnitId);
//    }

//    // Rewarded ad clicked callback
//    private void OnRewardedAdClicked(string adUnitId, MaxSdkBase.AdInfo info)
//    {
//        Debug.Log("Rewarded ad clicked: " + adUnitId);
//    }

//    // Rewarded ad hidden callback
//    private void OnRewardedAdHidden(string adUnitId, MaxSdkBase.AdInfo info)
//    {
//        Debug.Log("Rewarded ad hidden: " + adUnitId);
//    }

//    // Rewarded ad received reward callback
//    private void OnRewardedAdReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
//    {
//        string rewardName = reward.Label;
//        int rewardAmount = reward.Amount;
//        Debug.Log("Rewarded ad received reward: " + adUnitId + ", Reward name: " + rewardName + ", Reward amount: " + rewardAmount);
//        // TODO: Implement the logic to reward the player based on the received reward
//    }
//}
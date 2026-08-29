using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.DesignPattern;
    using Hung.Base;
    using global::Utilities;
    using Hung.Base.Init;
    using System;
    using Unity.Services.LevelPlay;
    using Hung.Utilities.Timer;
    using System.Collections;
    using Hung.Analytics;

    [DefaultExecutionOrder(-50)]
    public class AdsManager : Singleton<AdsManager>, IAdsService
    {
        public Action<float> _OnLoadingProgress { get; set; }
        protected int MAX_INIT_RETRY = 5;
        protected int currentRetry = 0;
        [SerializeField]
        GameBannerAds banner;
        [SerializeField]
        GameRewardAds reward;
        [SerializeField]
        GameInterAds interstitial;
        [SerializeField]
        AnalyticsManager analyticsManager;
        protected ADS_TYPE type;
        AdsLoadQueue loadQueue;
        bool isMaxAvailable = false;
        bool isIronSourceAvailable = false;
        string adsKey;
        STimer timer;

        //public bool IsBannerOpen => Banner.IsBannerOpen;
        public virtual IAds AppOpen => null;
        public IAds Banner => banner;
        public IRewardAds Reward => reward;
        public IInterAds Inter => interstitial;
        public ADS_TYPE Type
        {
            get => type;
            set
            {
                if (!IsAdsAvailable(value)) return;
                type = value;
                reward.Type = value;
                interstitial.Type = value;

                banner.Type = value;
                banner.InitBanner();
            }
        }

        public bool IsAdsAvailable(ADS_TYPE adsType)
        {
            bool value = false;
            switch (adsType)
            {
                case ADS_TYPE.MAX:
                    value = isMaxAvailable;
                    break;
                case ADS_TYPE.IRON_SOURCE:
                    value = isIronSourceAvailable;
                    break;
            }
            return value;
        }

        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (Locator.Pause == null)
            {
                float runningScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Locator.Pause = new PauseService(new UnityTimeScale(), runningScale);
            }
            Locator.Ads = this;
            timer = TimerManager.Ins.PopSTimer();
            currentRetry = 0;
#if UNITY_ANDROID
            adsKey = "24135de95";
#elif UNITY_IOS
            adsKey = "2493bb0ad";
#endif

            if (DebugManager.Ins && !DebugManager.Ins.IsShowAds) return;
            #region MAX
            loadQueue = new AdsLoadQueue(() => reward == null || !reward.IsShowingAds);
            isMaxAvailable = false;
            isIronSourceAvailable = false;
            // MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            // {
            //     //AppLovin SDK is initialized, start loading ads
            //     DevLog.Log(DevId.System, "Init Max Complete!!!");
            //     isMaxAvailable = true;
            //     // reward.LoadMaxAds();
            //     // OnAddLoadAds(interstitial.LoadMaxAds);
            //     // banner.InitBanner();
            //     StartRemoteDataCheck();
            // };
            // MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
            //MaxSdk.SetSdkKey("ZoNyqu_piUmpl33-qkoIfRp6MTZGW9M5xk1mb1ZIWK6FN9EBu0TXSHeprC3LMPQI7S3kTc1-x7DJGSV8S-gvFJ");
            // MaxSdk.InitializeSdk();
            #endregion
            // #region ADMOB
            // MobileAds.Initialize((InitializationStatus initStatus) =>
            // {
            //     //appOpenAds.Load();
            //     OnAddLoadAds(reward.LoadAdMobAds);
            //     OnAddLoadAds(interstitial.LoadAdsModAds);
            //     //banner.Init();
            //     DevLog.Log(DevId.Hung, "Init Ads Mob Complete!!!");
            // });
            // #endregion
            #region  IRON SOURCE
            DevLog.Log(DevId.System, $"INIT - {adsKey}");
            LevelPlay.OnInitSuccess += LevelPlayInitializationCompletedEvent;
            LevelPlay.OnInitFailed += LevelPlayInitializationFailedEvent;
            LevelPlay.OnImpressionDataReady += OnImpressionDataReady;
            LevelPlay.Init(adsKey);
            #endregion

            #region EVENTS
            reward._OnAddLoadAds += OnAddLoadAds;
            reward._OnTriggerLoadAds += OnTriggerLoadAds;

            interstitial._OnAddLoadAds += OnAddLoadAds;
            interstitial._OnTriggerLoadAds += OnTriggerLoadAds;
            #endregion
        }
        protected void StartRemoteDataCheck()
        {
            if (isIronSourceAvailable)
            {
                StartCoroutine(CheckingRemoteData());
            }
        }

        float loadingProgress = 0f;
        protected IEnumerator CheckingRemoteData()
        {
            while (FirebaseManager.Ins == null || !FirebaseManager.Ins.IsAvailable)
            {
                loadingProgress = Mathf.Lerp(loadingProgress, 1f, 0.05f);
                _OnLoadingProgress?.Invoke(loadingProgress);
                yield return null;
            }

            loadingProgress = 1f;
            _OnLoadingProgress?.Invoke(loadingProgress);

            // FirebaseManager.Ins.GetValueRemoteAsync("AdsConfig", (value) =>
            // {
            //     int adsType = 0;
            //     int.TryParse(value.StringValue.ToString(), out adsType);
            //     Type = (ADS_TYPE)adsType;
            //     DevLog.Log(DevId.System, $"Firebase Remote Config Ads Type: {Type} <==");
            // }); 
            yield return null;
            Type = ADS_TYPE.IRON_SOURCE;
            DevLog.Log(DevId.System, $"Firebase Remote Config Ads Type: {Type} <==");
            yield return null;

        }
        private void LevelPlayInitializationFailedEvent(LevelPlayInitError error)
        {
            DevLog.Log(DevId.System, "LevelPlay Initialization failed with error: " + error);

            if (currentRetry < MAX_INIT_RETRY)
            {
                currentRetry++;
                StartCoroutine(RetryInitLevelPlay());
            }
        }
        private IEnumerator RetryInitLevelPlay()
        {
            yield return new WaitForSecondsRealtime(2f * currentRetry);
            DevLog.Log(DevId.System, $"Retry INIT {currentRetry} - {adsKey}");
            LevelPlay.Init(adsKey);
        }
        private void LevelPlayInitializationCompletedEvent(LevelPlayConfiguration configuration)
        {
            DevLog.Log(DevId.System, "LevelPlay Initialization success with configuration: " + configuration);
            isIronSourceAvailable = true;
            StartRemoteDataCheck();
        }
        private void OnImpressionDataReady(LevelPlayImpressionData impressionData)
        {
            DevLog.Log(DevId.System, "OnImpressionDataReady: " + impressionData);
            if (impressionData == null || impressionData.Revenue <= 0)
                return;
            double revenue = Convert.ToDouble(impressionData.Revenue);

            Dictionary<string, string> extra = new Dictionary<string, string>
            {
                { "country", impressionData.Country },
                { "ad_unit", impressionData.MediationAdUnitId },
                { "ad_type", impressionData.AdFormat },
                { "placement", impressionData.Placement }
            };

            Locator.RevenueSink?.OnRevenue(impressionData.AdNetwork, revenue, "USD", extra);
        }
        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= LevelPlayInitializationCompletedEvent;
            LevelPlay.OnInitFailed -= LevelPlayInitializationFailedEvent;
            LevelPlay.OnImpressionDataReady -= OnImpressionDataReady;

            if (reward != null)
            {
                reward._OnAddLoadAds -= OnAddLoadAds;
                reward._OnTriggerLoadAds -= OnTriggerLoadAds;
            }

            if (interstitial != null)
            {
                interstitial._OnAddLoadAds -= OnAddLoadAds;
                interstitial._OnTriggerLoadAds -= OnTriggerLoadAds;
            }
        }
        protected void OnAddLoadAds(Action loadAction)
        {
            loadQueue.Enqueue(loadAction);
            DevLog.Log(DevId.Hung, $"Add Load Ads - {loadQueue.Count}");
        }
        protected void OnTriggerLoadAds()
        {
            loadQueue.MarkCurrentComplete();
            loadQueue.TryStartNext();
            DevLog.Log(DevId.Hung, $"Trigger Load Ads - {loadQueue.Count}");
        }
        private void OnApplicationPause(bool pause)
        {
            DevLog.Log(DevId.Hung, $"APPLICATION PAUSE: {pause}");
            //if (!pause)
            //{
            //    AppOpen.Show();
            //}
        }

        public void ShowRewarded(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            if (reward == null)
            {
                onCompleted?.Invoke(new AdsShowResult(request.RequestId, AdsRequestOutcome.Misconfigured, false, "reward-adapter-missing"));
                return;
            }
            reward.Show(request, onCompleted);
        }

        public void ShowInterstitial(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            if (interstitial == null)
            {
                onCompleted?.Invoke(new AdsShowResult(request.RequestId, AdsRequestOutcome.Misconfigured, false, "inter-adapter-missing"));
                return;
            }
            interstitial.Show(request, onCompleted);
        }
    }
}

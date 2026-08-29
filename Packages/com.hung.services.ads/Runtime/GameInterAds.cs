using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hung.DesignPattern;

namespace Hung.Ads
{
    using Hung.Base.Init;
    using Hung.Utilities.Timer;
    using System;
    using Hung.Base;
    using global::Utilities;
    using Hung.Utilities;

    public class GameInterAds : MonoBehaviour, IInterAds
    {
        public Action _OnTriggerLoadAds { get; set; }
        public Action<Action> _OnAddLoadAds { get; set; }
        public const int MAX_RETRY_ATTEMPT = 5;
        [Serializable]
        public struct ProviderBinding
        {
            public ADS_TYPE type;
            public MonoBehaviour behaviour;
        }

        [SerializeField]
        ProviderBinding[] providerBindings = new ProviderBinding[0];

        IAdsProviderRegistry registry;
        IInterstitialAdsProvider activeProvider;

        STimer cappingTimer;
        GameConfig config;
        // Lazy: Locator.Data is not guaranteed to be set when this Awake runs
        // (this GO survives scene loads via DontDestroyOnLoad).
        GameConfig Config => config ??= Locator.Data?.GetSOData<GameConfig>();
        readonly AdsRequestController requests = new AdsRequestController();
        AdsRequestContext activeRequest;
        InterstitialRequestSession activeSession;
        PauseLeaseId activePauseLeaseId;
        bool hasActivePauseLease;
        EventBinding<ResetInterCapEvent> _resetInterBinding;

        GameData gameData;
        GameData GameData => gameData ??= Locator.Data.GetData<GameData>();
        ADS_TYPE type;
        

        int adsMobTryAttempt = 0;
        int maxTryAttempt = 0;
        bool isShowOnLoad = false;
        bool isShowingAds = false;

        public ADS_TYPE Type
        {
            get => type;
            set
            {
                type = value;
                ResolveActiveProvider();
                if (activeProvider != null && !activeProvider.IsCanShow && !activeProvider.IsLoading)
                {
                    DevLog.Log(DevId.System, "[Inter Ads] Start Load!");
                    _OnAddLoadAds?.Invoke(LoadAds);
                }
            }
        }
        private void Awake()
        {
            cappingTimer = TimerManager.Ins.PopSTimer();
            _resetInterBinding = new EventBinding<ResetInterCapEvent>(ResetInterCapping);
            EventBus<ResetInterCapEvent>.Subscribe(_resetInterBinding);
            if (Config != null) cappingTimer.Start(Config.InterCappingTime);

            var built = new AdsProviderRegistry();
            for (int i = 0; i < providerBindings.Length; i++)
            {
                built.RegisterInterstitial(
                    providerBindings[i].type,
                    providerBindings[i].behaviour as IInterstitialAdsProvider);
            }
            ConfigureProviders(built);
        }

        private void OnDestroy()
        {
            UnsubscribeActive();
            EventBus<ResetInterCapEvent>.Unsubscribe(_resetInterBinding);
        }

        public void ConfigureProviders(IAdsProviderRegistry providerRegistry)
        {
            UnsubscribeActive();
            registry = providerRegistry;
            ResolveActiveProvider();
        }

        void ResolveActiveProvider()
        {
            UnsubscribeActive();
            if (registry == null)
            {
                activeProvider = null;
                return;
            }

            registry.TryGetInterstitial(type, out activeProvider);
            SubscribeActive();
        }

        void SubscribeActive()
        {
            if (activeProvider == null) return;
            activeProvider.OnAdsLoadFail += OnProviderLoadFail;
            activeProvider.OnAdsDone += OnProviderDone;
            activeProvider.OnAdsDisplayFail += OnProviderDisplayFail;
            activeProvider.OnAdsLoaded += OnProviderLoaded;
        }

        void UnsubscribeActive()
        {
            if (activeProvider == null) return;
            activeProvider.OnAdsLoadFail -= OnProviderLoadFail;
            activeProvider.OnAdsDone -= OnProviderDone;
            activeProvider.OnAdsDisplayFail -= OnProviderDisplayFail;
            activeProvider.OnAdsLoaded -= OnProviderLoaded;
            activeProvider = null;
        }
        public void Hide()
        {

        }

        public void Load()
        {
            activeProvider?.Load();
        }
        public void Show(Action callback, Placement placement = Placement.IN_GAME)
        {
            var id = AdsRequestId.Create("legacy-inter", AdsRequestKind.Interstitial, placement, Guid.NewGuid().ToString("N"));
            Show(new AdsShowRequest(id), result =>
            {
                if (result.ShouldContinueFlow) callback?.Invoke();
            });
        }

        public void Show(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            if (request.Kind != AdsRequestKind.Interstitial)
                throw new ArgumentException("Interstitial adapter requires an interstitial request.", nameof(request));

            isShowingAds = false;
            if (!requests.TryBegin(request, result =>
                {
                    activeRequest = null;
                    activeSession = null;
                    isShowingAds = false;
                    onCompleted?.Invoke(result);
                    ReleaseAdsLease();
                }, out var context, out var rejection))
            {
                onCompleted?.Invoke(rejection);
                return;
            }

            activeRequest = context;
            activeSession = new InterstitialRequestSession(context);
            DevLog.Log(DevId.System, "INTER: SHOW - Ads Request!");
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (GameData.IsRemoveAds())
                {
                    activeSession.OnSkipped("remove-ads");
                    Locator.Items.ShowBadge(GameData.IsPremiumRemoveAds() ? BaseItemIds.PremiumRemoveAds : BaseItemIds.RemoveAds);
                    DevLog.Log(DevId.System, "INTER: FAIL - Remove Ads!");
                }
                else
                {
                    if (!cappingTimer.IsStart
                        && Config != null
                        && GameData.user.normalLevelIndex >= Config.StartInterLevel)
                    {
                        if (Config.AdsCappingCount > 0
                        && GameData.user.watchingAdsCount > 0
                        && GameData.user.watchingAdsCount % Config.AdsCappingCount == 0)
                        {
                            activeSession.OnSkipped("inter-count-cap");
                            DevLog.Log(DevId.System, $"INTER: FAIL \n -Watch Ads Count:{GameData.user.watchingAdsCount} \n -Ads Capping:{Config.AdsCappingCount}!");
                            GameData.user.watchingAdsCount += 1;
                            gameData.user.playGameAdsCount = 0;
                            return;
                        }

                        if (gameData.user.playGameAdsCount % Config.ShowInterLevelStep == 0)
                        {
                            DevLog.Log(DevId.System, $"INTER: FAIL \n -Play Game Count:{gameData.user.playGameAdsCount} \n -Ads Capping:{Config.ShowInterLevelStep}!");
                            activeSession.OnSkipped("inter-level-step");
                            gameData.user.playGameAdsCount = 0;
                            return;
                        }

                        if (activeProvider != null && activeProvider.IsCanShow)
                        {
                            AcquireAdsLease(request);
                            activeProvider.Show(request.Placement);
                            if (Config != null) cappingTimer.Start(Config.InterCappingTime);
                            EventBus<ResetAoaCapEvent>.Raise(new ResetAoaCapEvent());
                            isShowOnLoad = false;
                            maxTryAttempt = 0;
                            isShowingAds = true;
                            GameData.user.watchingAdsCount += 1;
                        }
                        else
                        {
                            _OnAddLoadAds?.Invoke(LoadAds);
                            activeSession.OnUnavailable();
                        }
                    }
                    else
                    {
                        activeSession.OnSkipped(cappingTimer.IsStart ? "inter-time-cap" : "inter-start-level");
                        DevLog.Log(DevId.System, $"INTER: FAIL \n -Remaining Time:{cappingTimer.RemainingTime} \n -Level:{GameData.user.normalLevelIndex}!");
                    }
                }

            }
            else
            {
                activeSession.OnSkipped("debug-bypass");
            }
        }

        private void AcquireAdsLease(AdsShowRequest request)
        {
            var lease = new PauseLease(PauseLeaseId.Create(PauseLeaseKind.Ads, "ads", request.RequestId.Value), PauseLeaseKind.Ads, "ads");
            Locator.Pause?.Acquire(lease);
            activePauseLeaseId = lease.Id;
            hasActivePauseLease = true;
        }

        private void ReleaseAdsLease()
        {
            if (!hasActivePauseLease) return;
            Locator.Pause?.Release(activePauseLeaseId);
            hasActivePauseLease = false;
        }
        public void Show()
        {
            Show(null, Placement.IN_GAME);
        }
        protected void OnAdsReceiveReward()
        {
            MainThreadDispatcher.Ins.Enqueue(Action);
            maxTryAttempt = 0;
            adsMobTryAttempt = 0;
            isShowOnLoad = false;
            isShowingAds = false;

            void Action()
            {
                activeSession?.OnDone();
                EventBus<ResetInterCapEvent>.Raise(new ResetInterCapEvent());
            }
        }

        protected void OnAdsLoaded()
        {
            if (isShowOnLoad)
            {
                Show();
                isShowOnLoad = false;
            }
            _OnTriggerLoadAds?.Invoke();
        }

        internal void LoadAds()
        {
            if (activeProvider != null)
            {
                DevLog.Log(DevId.System, "[Inter Ads] Start Load!");
                activeProvider.Load();
            }
            else
            {
                _OnTriggerLoadAds?.Invoke();
            }
        }

        protected void OnProviderLoaded()
        {
            DevLog.Log(DevId.System, "[Inter Ads] Load Complete!");
            OnAdsLoaded();
        }

        protected void OnProviderLoadFail()
        {
            maxTryAttempt++;
            DevLog.Log(DevId.System, $"[Inter Ads] Load Fail! - {maxTryAttempt}");
            if (maxTryAttempt <= MAX_RETRY_ATTEMPT)
            {
                isShowOnLoad = true;
                _OnAddLoadAds?.Invoke(RetryLoad);
            }
            else
            {
                Locator.Analytics.AdsInterFail("Inter ads failed to load after maximum retries.");
                isShowOnLoad = false;
                maxTryAttempt = 0;
                activeSession?.OnUnavailable();
            }
            _OnTriggerLoadAds?.Invoke();

            void RetryLoad()
            {
                Invoke(nameof(LoadAds), 4f);
            }
        }

        protected void OnProviderDisplayFail()
        {
            maxTryAttempt++;
            DevLog.Log(DevId.System, $"[Inter Ads] Display Fail! - {maxTryAttempt}");
            isShowingAds = false;
            if (maxTryAttempt <= MAX_RETRY_ATTEMPT)
            {
                _OnAddLoadAds?.Invoke(LoadAds);
            }
            else
            {
                Locator.Analytics.AdsInterFail("Inter ads failed to load after maximum retries.");
                maxTryAttempt = 0;
                activeSession?.OnDisplayFailed();
            }
            _OnTriggerLoadAds?.Invoke();
        }

        protected void OnProviderDone()
        {
            DevLog.Log(DevId.System, "[Inter Ads] Done!");
            OnAdsReceiveReward();
            _OnAddLoadAds?.Invoke(LoadAds);
        }
        #region ADS MOB
        // internal void LoadAdsModAds()
        // {
        //     if(adsMobInterAds != null)
        //     {
        //         adsMobInterAds.Load();
        //         DevLog.Log(DevId.Hung, $"Load Ad Mob Inter Ads");
        //     }
        //     else
        //         _OnTriggerLoadAds?.Invoke();
        // }

        internal void OnAdsMobLoaded()
        {
            OnAdsLoaded();
        }
        // protected void OnAdsMobLoadFail(LoadAdError error)
        // {
        //     adsMobTryAttempt++;
        //     if (adsMobTryAttempt <= MAX_RETRY_ATTEMPT)
        //     {
        //         isShowOnLoad = true;
        //         _OnAddLoadAds.Invoke(LoadAds);
        //     }
        //     else
        //     {
        //         Locator.Analytics.AdsInterFail("Ads Mob inter ads failed to load after maximum retries.");
        //         isShowOnLoad = false;
        //         maxTryAttempt = 0;
        //         callback?.Invoke();
        //         callback = null;
        //     }
        //     _OnTriggerLoadAds?.Invoke();

        //     void LoadAds()
        //     {
        //         Invoke("LoadAdsModAds", 4f);
        //     }
        // }

        // protected void OnAdsMobDisplayFail()
        // {
        //     adsMobTryAttempt++;
        //     isShowingAds = false;
        //     if (adsMobTryAttempt <= MAX_RETRY_ATTEMPT)
        //     {
        //         _OnAddLoadAds?.Invoke(LoadAdsModAds);
        //     }
        //     else
        //     {
        //         Locator.Analytics.AdsInterFail("Ads Mobs ads failed to load after maximum retries.");
        //         maxTryAttempt = 0;
        //         callback?.Invoke();
        //         callback = null;
        //     }
        //     _OnTriggerLoadAds?.Invoke();

        // }
        // protected void OnAdsMobDone()
        // {
        //     OnAdsReceiveReward();
        //     _OnAddLoadAds?.Invoke(LoadAdsModAds);
        //     if (!maxInterAds.IsCanShow)
        //     {
        //         _OnAddLoadAds?.Invoke(LoadMaxAds);
        //     }

        // }
        #endregion
        private void ResetInterCapping()
        {
            if (Config != null) cappingTimer.Start(Config.InterCappingTime);
        }


    }
}

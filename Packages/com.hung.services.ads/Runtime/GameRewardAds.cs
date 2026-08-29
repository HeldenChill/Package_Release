using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Hung.DesignPattern;

namespace Hung.Ads
{
    using Hung.Base;
    using Hung.Base.Init;
    using System;
    using System.Diagnostics;
    using global::Utilities;
    using Hung.Utilities;
    using Hung.Utilities.Timer;

    public class GameRewardAds : MonoBehaviour, IRewardAds
    {
        public Action _OnTriggerLoadAds { get; set; }
        public Action<Action> _OnAddLoadAds { get; set; }
        public const int MAX_RETRY_ATTEMPT = 6;
        public const float RETRY_LOAD_TIME = 2f;
        [Serializable]
        public struct ProviderBinding
        {
            public ADS_TYPE type;
            public MonoBehaviour behaviour;
        }

        [SerializeField]
        ProviderBinding[] providerBindings = new ProviderBinding[0];

        IAdsProviderRegistry registry;
        IRewardedAdsProvider activeProvider;

        Placement placement;
        readonly AdsRequestController requests = new AdsRequestController();
        AdsRequestContext activeRequest;
        RewardedRequestSession activeSession;
        PauseLeaseId activePauseLeaseId;
        bool hasActivePauseLease;

        int adsMobTryAttempt = 0;
        int maxTryAttempt = 0;
        bool isShowOnLoad = false;
        bool isShowingAds = false;
        protected GameData gameData = null;
        protected ADS_TYPE type;
        GameData GameData => gameData ??= Locator.Data.GetData<GameData>();

        public bool IsShowingAds => isShowingAds;

        public ADS_TYPE Type
        {
            get => type;
            set
            {
                type = value;
                ResolveActiveProvider();
                if (activeProvider != null && !activeProvider.IsCanShow && !activeProvider.IsLoading)
                {
                    _OnAddLoadAds?.Invoke(LoadAds);
                }
            }
        }

        private void Awake()
        {
            var built = new AdsProviderRegistry();
            for (int i = 0; i < providerBindings.Length; i++)
            {
                built.RegisterRewarded(
                    providerBindings[i].type,
                    providerBindings[i].behaviour as IRewardedAdsProvider);
            }
            ConfigureProviders(built);
        }

        private void OnDestroy()
        {
            UnsubscribeActive();
        }

        // Called from Awake, and directly by tests with a prebuilt registry.
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

            registry.TryGetRewarded(type, out activeProvider);
            SubscribeActive();
        }

        void SubscribeActive()
        {
            if (activeProvider == null) return;
            activeProvider.OnAdsLoadFail += OnProviderLoadFail;
            activeProvider.OnAdsReceiveReward += OnProviderReceiveReward;
            activeProvider.OnAdsHidden += OnProviderHidden;
            activeProvider.OnAdsDisplayFail += OnProviderDisplayFail;
            activeProvider.OnAdsLoaded += OnProviderLoaded;
        }

        void UnsubscribeActive()
        {
            if (activeProvider == null) return;
            activeProvider.OnAdsLoadFail -= OnProviderLoadFail;
            activeProvider.OnAdsReceiveReward -= OnProviderReceiveReward;
            activeProvider.OnAdsHidden -= OnProviderHidden;
            activeProvider.OnAdsDisplayFail -= OnProviderDisplayFail;
            activeProvider.OnAdsLoaded -= OnProviderLoaded;
            activeProvider = null;
        }
        public void Hide()
        {

        }

        public void Load()
        {
        }

        public void Show()
        {
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (GameData.IsPremiumRemoveAds())
                {
                    Show(null, null, placement);
                    DevLog.Log(DevId.System, "REWARD: FAIL - Premium Remove Ads!");
                }
                else
                {
                    Show(null, null, placement);
                }
            }
            else
            {
                Show(null, null, placement);
                DevLog.Log(DevId.System, "REWARD: FAIL - Premium Remove Ads!");
            }
        }

        public void Show(Action rewardCallback, Action hiddenCallback = null, Placement placement = Placement.NONE)
        {
            var id = AdsRequestId.Create("legacy-reward", AdsRequestKind.Rewarded, placement, Guid.NewGuid().ToString("N"));
            Show(new AdsShowRequest(id), result =>
            {
                if (result.IsEarnedReward || result.DiagnosticCode == "premium-bypass" || result.DiagnosticCode == "debug-bypass")
                {
                    rewardCallback?.Invoke();
                }

                if (result.ShouldContinueFlow)
                {
                    hiddenCallback?.Invoke();
                }
            });
        }

        public void Show(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            if (request.Kind != AdsRequestKind.Rewarded)
                throw new ArgumentException("Rewarded adapter requires a rewarded request.", nameof(request));

            this.placement = request.Placement;
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
            activeSession = new RewardedRequestSession(context);

            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (GameData.IsPremiumRemoveAds())
                {
                    activeRequest.Complete(AdsRequestOutcome.Skipped, "premium-bypass");
                    Locator.Items.ShowBadge(BaseItemIds.PremiumRemoveAds);
                    DevLog.Log(DevId.System, "REWARD: FAIL - Premium Remove Ads!");
                }
                else
                {
                    if (activeProvider != null && activeProvider.IsCanShow)
                    {
                        AcquireAdsLease(request);
                        activeProvider.Show(request.Placement);
                        EventBus<ResetAoaCapEvent>.Raise(new ResetAoaCapEvent());
                        maxTryAttempt = 0;
                        isShowOnLoad = false;
                        isShowingAds = true;
                        GameData.user.watchingAdsCount += 1;
                    }
                    else
                    {
                        isShowOnLoad = true;
                        DevLog.Log(DevId.Hung, "Reward Ads is not ready to show. Attempting to load.");
                        _OnAddLoadAds?.Invoke(LoadAds);
                        activeSession.OnUnavailable();
                    }
                }
            }
            else
            {
                activeRequest.Complete(AdsRequestOutcome.Skipped, "debug-bypass");
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

        protected void OnAdsReceiveReward()
        {
            MainThreadDispatcher.Ins.Enqueue(Action);
            maxTryAttempt = 0;
            adsMobTryAttempt = 0;
            isShowOnLoad = false;

            void Action()
            {
                activeSession?.OnRewardEarned("rewarded-provider");
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

        protected void OnAddDone()
        {
            activeSession?.OnHidden();
            isShowingAds = false;
            _OnTriggerLoadAds?.Invoke();
        }
        internal void LoadAds()
        {
            if (activeProvider != null)
            {
                DevLog.Log(DevId.System, "[Reward Ads] Start Load!");
                activeProvider.Load();
            }
            else
            {
                _OnTriggerLoadAds?.Invoke();
            }
        }

        protected void OnProviderLoaded()
        {
            DevLog.Log(DevId.System, "[Reward Ads] Load Complete!");
            OnAdsLoaded();
            Locator.Analytics.AdsRewardLoadComplete();
        }

        protected void OnProviderLoadFail()
        {
            maxTryAttempt++;
            DevLog.Log(DevId.System, $"[Reward Ads] Load Fail! - {maxTryAttempt}");
            if (maxTryAttempt <= MAX_RETRY_ATTEMPT)
            {
                isShowOnLoad = true;
                _OnAddLoadAds?.Invoke(RetryLoad);
            }
            else
            {
                Locator.Analytics.AdsRewardLoadFail();
                activeSession?.OnUnavailable();
                maxTryAttempt = 0;
            }
            _OnTriggerLoadAds?.Invoke();

            void RetryLoad()
            {
                Invoke(nameof(LoadAds), 3f);
            }
        }

        protected void OnProviderDisplayFail()
        {
            maxTryAttempt++;
            DevLog.Log(DevId.System, $"[Reward Ads] Display Fail! - {maxTryAttempt}");
            isShowingAds = false;
            if (maxTryAttempt <= MAX_RETRY_ATTEMPT)
            {
                _OnAddLoadAds?.Invoke(LoadAds);
            }
            else
            {
                Locator.Analytics.AdsRewardShowFail(Placement.NONE, "");
                activeSession?.OnDisplayFailed();
                maxTryAttempt = 0;
            }
            _OnTriggerLoadAds?.Invoke();
        }

        protected void OnProviderReceiveReward()
        {
            DevLog.Log(DevId.System, "[Reward Ads] Reward!");
            OnAdsReceiveReward();
            _OnAddLoadAds?.Invoke(LoadAds);
        }

        private void OnProviderHidden()
        {
            DevLog.Log(DevId.System, "[Reward Ads] Hidden!");
            OnAddDone();
        }
        #region ADS MOB
        // internal void LoadAdMobAds()
        // {
        //     if(adMobRewardAds != null)
        //     {
        //         adMobRewardAds.Load();
        //         DevLog.Log(DevId.Hung, $"Load Ad Mob Reward Ads");
        //     }
        //     else
        //         _OnTriggerLoadAds?.Invoke();
        // }
        internal void OnAdsMobLoaded()
        {
            OnAdsLoaded();
            Locator.Analytics.AdsRewardLoadComplete();  
        }
        // protected void OnAdsMobLoadFail(LoadAdError error)
        // {
        //     adsMobTryAttempt++;
        //     if (adsMobTryAttempt <= MAX_RETRY_ATTEMPT)
        //     {
        //         isShowOnLoad = true;
        //         _OnAddLoadAds?.Invoke(LoadAds);
        //     }
        //     else
        //     {
        //         AnalysticManager.Ins.AdsRewardLoadFail();
        //         hiddenCallback?.Invoke();
        //         hiddenCallback = null;
        //         maxTryAttempt = 0; // Reset attempt count for future load attempts
        //     }
        //     _OnTriggerLoadAds?.Invoke();

        //     void LoadAds()
        //     {
        //         Invoke("LoadAdMobAds", 3f);
        //     }
        // }

        // protected void OnAdsMobDisplayFail()
        // {
        //     adsMobTryAttempt++;
        //     isShowingAds = false; 
        //     if (adsMobTryAttempt <= MAX_RETRY_ATTEMPT)
        //     {
        //         _OnAddLoadAds?.Invoke(LoadAdMobAds);
        //     }
        //     else
        //     {
        //         AnalysticManager.Ins.AdsRewardShowFail(Placement.NONE, "Ads Mob reward ads failed to load after maximum retries.");
        //         hiddenCallback?.Invoke();
        //         hiddenCallback = null;
        //         maxTryAttempt = 0; // Reset attempt count for future load attempts
        //     }
        //     _OnTriggerLoadAds?.Invoke();
        // }
        // protected void OnAdsMobReceiveReward()
        // {
        //     OnAdsReceiveReward();
        //     _OnAddLoadAds?.Invoke(LoadAdMobAds);
        //     if (!maxRewardedAds.IsCanShow)
        //     {
        //         _OnAddLoadAds?.Invoke(LoadMaxAds);
        //     }
        // }
        private void OnAdsMobHidden()
        {
            MainThreadDispatcher.Ins.Enqueue(OnAddDone);
        }
        #endregion
    }
}

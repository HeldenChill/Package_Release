using System;

namespace Hung.Ads
{
    using Hung.Base;

    // Ph6 test double (paper §17.7): all-no-op IAdsService for EditMode/PlayMode
    // tests and offline/vendor-free builds. Never calls a mediation SDK.
    public class NullAdsService : IAdsService
    {
        public IAds AppOpen { get; } = new NullAds();
        public IAds Banner { get; } = new NullAds();
        public IRewardAds Reward { get; } = new NullRewardAds();
        public IInterAds Inter { get; } = new NullInterAds();
        public ADS_TYPE Type { get; set; }

        public void ShowRewarded(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            onCompleted?.Invoke(new AdsShowResult(request.RequestId, AdsRequestOutcome.Unsupported, false, "null-service"));
        }

        public void ShowInterstitial(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            onCompleted?.Invoke(new AdsShowResult(request.RequestId, AdsRequestOutcome.Unsupported, false, "null-service"));
        }

        private class NullAds : IAds
        {
            public ADS_TYPE Type { get; set; }
            public void Show() { }
            public void Hide() { }
            public void Load() { }
        }

        private class NullRewardAds : IRewardAds
        {
            public ADS_TYPE Type { get; set; }
            public Action _OnTriggerLoadAds => null;
            public Action<Action> _OnAddLoadAds => null;
            public void Show() { }
            public void Hide() { }
            public void Load() { }
            public void Show(Action rewardCallBack, Action hiddenCallBack = null, Placement placement = Placement.NONE)
            {
                hiddenCallBack?.Invoke();
            }
        }

        private class NullInterAds : IInterAds
        {
            public ADS_TYPE Type { get; set; }
            public Action _OnTriggerLoadAds => null;
            public Action<Action> _OnAddLoadAds => null;
            public void Show() { }
            public void Hide() { }
            public void Load() { }
            public void Show(Action callback, Placement placement) => callback?.Invoke();
        }
    }
}

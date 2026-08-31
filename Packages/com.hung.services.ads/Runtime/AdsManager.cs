using System;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;
    using Hung.DesignPattern;

    // Neutral composition root for the four Game*Ads components. A game wires this once (a
    // prefab with references to its GameAppOpenAds/GameBannerAds/GameRewardAds/GameInterAds
    // instances) and registers PvmLocator.Ads = AdsManager.Ins - every consumer then goes
    // through IAdsService without knowing which vendor is behind it.
    public sealed class AdsManager : Singleton<AdsManager>, IAdsService
    {
        [SerializeField]
        MonoBehaviour appOpenBehaviour;
        [SerializeField]
        MonoBehaviour bannerBehaviour;
        [SerializeField]
        MonoBehaviour rewardBehaviour;
        [SerializeField]
        MonoBehaviour interBehaviour;

        IAds appOpen;
        IAds banner;
        IRewardAds reward;
        IInterAds inter;

        public IAds AppOpen => appOpen;
        public IAds Banner => banner;
        public IRewardAds Reward => reward;
        public IInterAds Inter => inter;

        public ADS_TYPE Type
        {
            get => reward != null ? reward.Type : default;
            set
            {
                if (appOpen != null) appOpen.Type = value;
                if (banner != null) banner.Type = value;
                if (reward != null) reward.Type = value;
                if (inter != null) inter.Type = value;
            }
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            appOpen = appOpenBehaviour as IAds;
            banner = bannerBehaviour as IAds;
            reward = rewardBehaviour as IRewardAds;
            inter = interBehaviour as IInterAds;

            // Self-registration: the consuming game holds IAdsService (a Hung.Base contract),
            // so it never needs an assembly reference to Hung.Ads. See the class comment.
            Locator.Ads = this;
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Locator.Ads, this))
            {
                Locator.Ads = null;
            }
        }

        public void ShowRewarded(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            if (reward == null) return;
            (reward as GameRewardAds)?.Show(request, onCompleted);
        }

        public void ShowInterstitial(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            if (inter == null) return;
            (inter as GameInterAds)?.Show(request, onCompleted);
        }
    }
}

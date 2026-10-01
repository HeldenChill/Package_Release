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

        [SerializeField]
        [Tooltip("Optional. When set, rewarded/interstitial/banner are routed across installed providers per this config, and the Game*Ads providerBindings are ignored.")]
        AdsRoutingConfig routingConfig;

        [Header("Enabled Ad Formats")]
        [SerializeField, Tooltip("Allow rewarded loading and showing. Disabled requests skip without granting a reward.")]
        bool rewardedAdsEnabled = true;
        [SerializeField, Tooltip("Allow interstitial loading and showing.")]
        bool interstitialAdsEnabled = true;
        [SerializeField, Tooltip("Allow banner initialization and showing.")]
        bool bannerAdsEnabled = true;

        AdsRoutedComposer.Result routed;

        /// <summary>Optional remote routing override (JSON, see AdsRoutingOverride). Read at every load cycle. Reset on SubsystemRegistration.</summary>
        public static Func<string> RoutingOverrideJson { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => RoutingOverrideJson = null;

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
            ApplyFormatSettings();

            // Self-registration: the consuming game holds IAdsService (a Hung.Base contract),
            // so it never needs an assembly reference to Hung.Ads. See the class comment.
            Locator.Ads = this;
        }

        void ApplyFormatSettings()
        {
            if (reward is GameRewardAds rewarded) rewarded.AdsEnabled = rewardedAdsEnabled;
            if (inter is GameInterAds interstitial) interstitial.AdsEnabled = interstitialAdsEnabled;
            if (banner is GameBannerAds bannerAds) bannerAds.AdsEnabled = bannerAdsEnabled;
        }

        void Start()
        {
            ApplyFormatSettings();
            if (routingConfig == null) return;

            routed = AdsRoutedComposer.Compose(routingConfig.Data, AdsInstallers.All, gameObject, () => RoutingOverrideJson?.Invoke());
            var registry = routed.ToLegacyRegistry();
            (reward as GameRewardAds)?.ConfigureProviders(registry);
            (inter as GameInterAds)?.ConfigureProviders(registry);
            (banner as GameBannerAds)?.ConfigureProviders(registry);

            // Re-assigning Type runs the existing "load if not ready" path through the Game*Ads lifecycle.
            if (reward != null) reward.Type = reward.Type;
            if (inter != null) inter.Type = inter.Type;
        }

        void OnDestroy()
        {
            routed?.Dispose();
            routed = null;
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

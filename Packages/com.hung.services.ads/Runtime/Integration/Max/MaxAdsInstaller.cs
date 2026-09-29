using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    using Hung.Ads;

    /// <summary>
    /// Registers MAX providers for routed ads. MAX SDK initialisation stays with the game, as before.
    /// A format is installed only when its ad unit id is set in the routing config.
    /// </summary>
    public sealed class MaxAdsInstaller : IAdsProviderInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register() => AdsInstallers.Register(new MaxAdsInstaller());

        public string ProviderId => AdsProviderId.Max;

        public void Install(GameObject host, AdsProviderEntry entry, AdsProviderSet set)
        {
            string unit = entry.UnitFor(AdsFormat.Rewarded);
            if (unit != null)
            {
                var rewarded = host.AddComponent<RewardedAds>();
                rewarded.SetAdUnitId(unit);
                set.AddRewarded(ProviderId, rewarded);
            }

            unit = entry.UnitFor(AdsFormat.Interstitial);
            if (unit != null)
            {
                var inter = host.AddComponent<InterstitialAds>();
                inter.SetAdUnitId(unit);
                set.AddInterstitial(ProviderId, inter);
            }

            unit = entry.UnitFor(AdsFormat.Banner);
            if (unit != null)
            {
                var banner = host.AddComponent<BannerAds>();
                banner.SetAdUnitId(unit);
                set.AddBanner(ProviderId, banner);
            }
        }
    }
}

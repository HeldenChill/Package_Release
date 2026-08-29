using System.Collections.Generic;

namespace Hung.Ads
{
    using Hung.Base;

    public sealed class AdsProviderRegistry : IAdsProviderRegistry
    {
        private readonly Dictionary<ADS_TYPE, IBannerAdsProvider> banners = new();
        private readonly Dictionary<ADS_TYPE, IInterstitialAdsProvider> interstitials = new();
        private readonly Dictionary<ADS_TYPE, IRewardedAdsProvider> rewarded = new();

        public void RegisterBanner(ADS_TYPE type, IBannerAdsProvider provider)
        {
            if (provider == null) return;
            banners[type] = provider;
        }

        public void RegisterInterstitial(ADS_TYPE type, IInterstitialAdsProvider provider)
        {
            if (provider == null) return;
            interstitials[type] = provider;
        }

        public void RegisterRewarded(ADS_TYPE type, IRewardedAdsProvider provider)
        {
            if (provider == null) return;
            rewarded[type] = provider;
        }

        public bool TryGetBanner(ADS_TYPE type, out IBannerAdsProvider provider)
            => banners.TryGetValue(type, out provider);

        public bool TryGetInterstitial(ADS_TYPE type, out IInterstitialAdsProvider provider)
            => interstitials.TryGetValue(type, out provider);

        public bool TryGetRewarded(ADS_TYPE type, out IRewardedAdsProvider provider)
            => rewarded.TryGetValue(type, out provider);
    }
}

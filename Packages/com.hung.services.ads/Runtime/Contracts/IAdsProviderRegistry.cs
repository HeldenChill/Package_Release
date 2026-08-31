namespace Hung.Ads
{
    using Hung.Base;

    // Vendor-agnostic provider lookup. The neutral Game*Ads classes resolve their
    // active provider through this registry instead of holding per-vendor fields,
    // so adding or swapping a mediation vendor never edits the neutral core.
    public interface IAdsProviderRegistry
    {
        void RegisterBanner(ADS_TYPE type, IBannerAdsProvider provider);
        void RegisterInterstitial(ADS_TYPE type, IInterstitialAdsProvider provider);
        void RegisterRewarded(ADS_TYPE type, IRewardedAdsProvider provider);

        bool TryGetBanner(ADS_TYPE type, out IBannerAdsProvider provider);
        bool TryGetInterstitial(ADS_TYPE type, out IInterstitialAdsProvider provider);
        bool TryGetRewarded(ADS_TYPE type, out IRewardedAdsProvider provider);
    }
}

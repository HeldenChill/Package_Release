using System.Collections.Generic;

namespace Hung.Ads
{
    /// <summary>Installed vendor providers keyed by provider id (see AdsProviderId).</summary>
    public sealed class AdsProviderSet
    {
        readonly Dictionary<string, IRewardedAdsProvider> rewarded = new Dictionary<string, IRewardedAdsProvider>();
        readonly Dictionary<string, IInterstitialAdsProvider> interstitial = new Dictionary<string, IInterstitialAdsProvider>();
        readonly Dictionary<string, IBannerAdsProvider> banner = new Dictionary<string, IBannerAdsProvider>();

        public void AddRewarded(string id, IRewardedAdsProvider provider) { if (!string.IsNullOrEmpty(id) && provider != null) rewarded[id] = provider; }
        public void AddInterstitial(string id, IInterstitialAdsProvider provider) { if (!string.IsNullOrEmpty(id) && provider != null) interstitial[id] = provider; }
        public void AddBanner(string id, IBannerAdsProvider provider) { if (!string.IsNullOrEmpty(id) && provider != null) banner[id] = provider; }

        public bool TryGetRewarded(string id, out IRewardedAdsProvider provider) => rewarded.TryGetValue(id ?? string.Empty, out provider);
        public bool TryGetInterstitial(string id, out IInterstitialAdsProvider provider) => interstitial.TryGetValue(id ?? string.Empty, out provider);
        public bool TryGetBanner(string id, out IBannerAdsProvider provider) => banner.TryGetValue(id ?? string.Empty, out provider);

        public IEnumerable<IRewardedAdsProvider> AllRewarded => rewarded.Values;
        public IEnumerable<IInterstitialAdsProvider> AllInterstitial => interstitial.Values;
        public IEnumerable<IBannerAdsProvider> AllBanner => banner.Values;

        /// <summary>True when a provider with this id is installed for this format.</summary>
        public bool Has(string id, AdsFormat format)
        {
            if (string.IsNullOrEmpty(id)) return false;
            switch (format)
            {
                case AdsFormat.Rewarded: return rewarded.ContainsKey(id);
                case AdsFormat.Interstitial: return interstitial.ContainsKey(id);
                default: return banner.ContainsKey(id);
            }
        }
    }
}

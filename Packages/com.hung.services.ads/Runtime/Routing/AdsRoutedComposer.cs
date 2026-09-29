using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>Builds the routed providers from config + installers. Used by AdsManager when a routing config is assigned.</summary>
    public static class AdsRoutedComposer
    {
        /// <summary>Everything routed mode needs; dispose to unsubscribe from vendor providers.</summary>
        public sealed class Result : IDisposable
        {
            public AdsRouter Router;
            public AdsProviderSet Set;
            public RoutedRewardedProvider Rewarded;
            public RoutedInterstitialProvider Interstitial;
            public RoutedBannerProvider Banner;

            /// <summary>Registry holding the routed providers under every ADS_TYPE, so Game*Ads work unchanged whatever Type is set.</summary>
            public AdsProviderRegistry ToLegacyRegistry()
            {
                var registry = new AdsProviderRegistry();
                foreach (ADS_TYPE t in Enum.GetValues(typeof(ADS_TYPE)))
                {
                    registry.RegisterRewarded(t, Rewarded);
                    registry.RegisterInterstitial(t, Interstitial);
                    registry.RegisterBanner(t, Banner);
                }
                return registry;
            }

            public void Dispose()
            {
                Rewarded?.Dispose();
                Interstitial?.Dispose();
                Banner?.Dispose();
            }
        }

        public static Result Compose(AdsRoutingData data, IEnumerable<IAdsProviderInstaller> installers, GameObject host, Func<string> overrideJson)
        {
            data = data ?? new AdsRoutingData();
            var set = new AdsProviderSet();
            foreach (var installer in installers)
            {
                var entry = data.FindProvider(installer.ProviderId);
                if (entry == null)
                {
                    Debug.LogWarning("[Ads Routing] Provider '" + installer.ProviderId + "' is compiled in but has no entry in the routing config; skipped.");
                    continue;
                }
                installer.Install(host, entry, set);
            }

            var router = new AdsRouter(data, set.Has, overrideJson);
            return new Result
            {
                Router = router,
                Set = set,
                Rewarded = new RoutedRewardedProvider(router, set),
                Interstitial = new RoutedInterstitialProvider(router, set),
                Banner = new RoutedBannerProvider(router, set),
            };
        }
    }
}

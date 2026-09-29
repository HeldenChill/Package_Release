using NUnit.Framework;
using UnityEngine;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class GameRewardAdsCanShowForTests
    {
        GameObject go;
        [TearDown] public void TearDown() { if (go != null) Object.DestroyImmediate(go); }

        [Test]
        public void Routed_DelegatesToPlacementRoute()
        {
            var data = RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max");
            data.placements.Add(new AdsPlacementRoute { format = AdsFormat.Rewarded, placement = Placement.SPIN, mode = AdsRouteMode.Chain, order = new System.Collections.Generic.List<string> { "admob" } });
            var max = new FakeRewarded("max") { Ready = true };
            var admob = new FakeRewarded("admob");
            var set = new AdsProviderSet();
            set.AddRewarded("max", max);
            set.AddRewarded("admob", admob);
            var routed = new RoutedRewardedProvider(new AdsRouter(data, set.Has), set);
            var registry = new AdsProviderRegistry();
            registry.RegisterRewarded(ADS_TYPE.MAX, routed);

            go = new GameObject("reward");
            var ads = go.AddComponent<GameRewardAds>();
            ads.ConfigureProviders(registry);

            IRewardAds contract = ads;
            Assert.IsTrue(contract.IsCanShowFor(Placement.X2_COIN));
            Assert.IsFalse(contract.IsCanShowFor(Placement.SPIN));
        }

        [Test]
        public void Legacy_UsesActiveProviderReadiness()
        {
            var registry = new AdsProviderRegistry();
            registry.RegisterRewarded(ADS_TYPE.MAX, new FakeRewarded("max") { Ready = true });
            go = new GameObject("reward");
            var ads = go.AddComponent<GameRewardAds>();
            ads.ConfigureProviders(registry);
            Assert.IsTrue(((IRewardAds)ads).IsCanShowFor(Placement.SPIN));
        }
    }
}

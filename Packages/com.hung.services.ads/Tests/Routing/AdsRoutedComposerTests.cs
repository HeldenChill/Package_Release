using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class AdsRoutedComposerTests
    {
        GameObject host;
        [SetUp] public void SetUp() => host = new GameObject("ads");
        [TearDown] public void TearDown() => Object.DestroyImmediate(host);

        sealed class FakeInstaller : IAdsProviderInstaller
        {
            public readonly FakeRewarded Rewarded;
            public int Installs;
            public FakeInstaller(string id) { ProviderId = id; Rewarded = new FakeRewarded(id); }
            public string ProviderId { get; }
            public void Install(GameObject h, AdsProviderEntry entry, AdsProviderSet set)
            {
                Installs++;
                set.AddRewarded(ProviderId, Rewarded);
            }
        }

        [Test]
        public void InstallersWithEntry_Installed_EvenWhenDisabled()
        {
            var data = RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max", "admob");
            data.FindProvider("admob").enabled = false;
            var max = new FakeInstaller("max");
            var admob = new FakeInstaller("admob");
            var result = AdsRoutedComposer.Compose(data, new IAdsProviderInstaller[] { max, admob }, host, null);
            Assert.AreEqual(1, admob.Installs, "installed so remote override can enable it");
            CollectionAssert.AreEqual(new[] { "max" }, result.Router.Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds);
        }

        [Test]
        public void InstallerWithoutEntry_Skipped_WithWarning()
        {
            var data = RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max");
            var unity = new FakeInstaller("unity");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unity"));
            AdsRoutedComposer.Compose(data, new IAdsProviderInstaller[] { unity }, host, null);
            Assert.AreEqual(0, unity.Installs);
        }

        [Test]
        public void LegacyRegistry_ExposesRoutedUnderEveryAdsType()
        {
            var result = AdsRoutedComposer.Compose(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"),
                new IAdsProviderInstaller[] { new FakeInstaller("max") }, host, null);
            var registry = result.ToLegacyRegistry();
            foreach (ADS_TYPE t in System.Enum.GetValues(typeof(ADS_TYPE)))
            {
                Assert.IsTrue(registry.TryGetRewarded(t, out var r));
                Assert.AreSame(result.Rewarded, r);
                Assert.IsTrue(registry.TryGetInterstitial(t, out var i));
                Assert.AreSame(result.Interstitial, i);
                Assert.IsTrue(registry.TryGetBanner(t, out var b));
                Assert.AreSame(result.Banner, b);
            }
        }

        [Test]
        public void OverrideJson_ReadThroughRouter()
        {
            string json = "{\"providers\":[{\"id\":\"max\",\"enabled\":false}]}";
            var result = AdsRoutedComposer.Compose(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"),
                new IAdsProviderInstaller[] { new FakeInstaller("max") }, host, () => json);
            Assert.IsTrue(result.Router.Resolve(AdsFormat.Rewarded, Placement.NONE).IsEmpty);
        }
    }
}

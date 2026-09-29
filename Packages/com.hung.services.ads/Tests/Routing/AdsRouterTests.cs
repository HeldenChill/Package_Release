using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class AdsRouterTests
    {
        static AdsRoutingData Data() => new AdsRoutingData
        {
            providers = new List<AdsProviderEntry>
            {
                new AdsProviderEntry { id = "max" },
                new AdsProviderEntry { id = "admob" },
                new AdsProviderEntry { id = "yandex" },
            },
            formats = new List<AdsFormatRoute>
            {
                new AdsFormatRoute { format = AdsFormat.Rewarded, mode = AdsRouteMode.Chain, order = new List<string> { "max", "admob", "yandex" } },
                new AdsFormatRoute { format = AdsFormat.Banner, mode = AdsRouteMode.Parallel, order = new List<string> { "admob" } },
            },
            placements = new List<AdsPlacementRoute>
            {
                new AdsPlacementRoute { format = AdsFormat.Rewarded, placement = Placement.REROLL_SKILL_CARD, mode = AdsRouteMode.Parallel, order = new List<string> { "yandex" } },
            },
        };

        static bool All(string id, AdsFormat f) => true;

        [Test]
        public void Resolve_NoPlacementOverride_UsesFormatRoute()
        {
            var route = new AdsRouter(Data(), All).Resolve(AdsFormat.Rewarded, Placement.X2_COIN);
            Assert.AreEqual(AdsRouteMode.Chain, route.Mode);
            CollectionAssert.AreEqual(new[] { "max", "admob", "yandex" }, route.ProviderIds);
        }

        [Test]
        public void Resolve_PlacementOverride_Wins()
        {
            var route = new AdsRouter(Data(), All).Resolve(AdsFormat.Rewarded, Placement.REROLL_SKILL_CARD);
            Assert.AreEqual(AdsRouteMode.Parallel, route.Mode);
            CollectionAssert.AreEqual(new[] { "yandex" }, route.ProviderIds);
        }

        [Test]
        public void Resolve_DropsDisabledProvider()
        {
            var data = Data();
            data.FindProvider("max").enabled = false;
            CollectionAssert.AreEqual(new[] { "admob", "yandex" }, new AdsRouter(data, All).Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds);
        }

        [Test]
        public void Resolve_DropsNotInstalledProvider()
        {
            var route = new AdsRouter(Data(), (id, f) => id != "admob").Resolve(AdsFormat.Rewarded, Placement.NONE);
            CollectionAssert.AreEqual(new[] { "max", "yandex" }, route.ProviderIds);
        }

        [Test]
        public void Resolve_DropsIdMissingFromProviders_AndDuplicates()
        {
            var data = Data();
            data.formats[0].order = new List<string> { "max", "ghost", "max", "", null };
            CollectionAssert.AreEqual(new[] { "max" }, new AdsRouter(data, All).Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds);
        }

        [Test]
        public void Resolve_NoRouteForFormat_ReturnsEmpty()
        {
            Assert.IsTrue(new AdsRouter(Data(), All).Resolve(AdsFormat.Interstitial, Placement.NONE).IsEmpty);
        }

        [Test]
        public void Resolve_BannerParallel_CoercedToChain_WarnsOnce()
        {
            var router = new AdsRouter(Data(), All);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Banner"));
            Assert.AreEqual(AdsRouteMode.Chain, router.Resolve(AdsFormat.Banner, Placement.NONE).Mode);
            Assert.AreEqual(AdsRouteMode.Chain, router.Resolve(AdsFormat.Banner, Placement.NONE).Mode);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PreloadRoutes_DefaultFirst_ThenPlacementRoutes()
        {
            var routes = new AdsRouter(Data(), All).PreloadRoutes(AdsFormat.Rewarded);
            Assert.AreEqual(2, routes.Count);
            CollectionAssert.AreEqual(new[] { "max", "admob", "yandex" }, routes[0].ProviderIds);
            CollectionAssert.AreEqual(new[] { "yandex" }, routes[1].ProviderIds);
        }

        [Test]
        public void Override_Applied_AndReadEachResolve()
        {
            string json = null;
            var router = new AdsRouter(Data(), All, () => json);
            Assert.AreEqual("max", router.Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds[0]);
            json = "{\"formats\":[{\"format\":\"Rewarded\",\"mode\":\"Chain\",\"order\":[\"admob\"]}]}";
            CollectionAssert.AreEqual(new[] { "admob" }, router.Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds);
        }

        [Test]
        public void Override_Malformed_KeepsBaseline()
        {
            var router = new AdsRouter(Data(), All, () => "{\"formats\":[{\"format\":\"Nope\"}]}");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Override rejected"));
            CollectionAssert.AreEqual(new[] { "max", "admob", "yandex" }, router.Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds);
        }

        [Test]
        public void Override_UnknownId_Dropped()
        {
            var router = new AdsRouter(Data(), All, () => "{\"formats\":[{\"format\":\"Rewarded\",\"mode\":\"Chain\",\"order\":[\"unity\",\"admob\"]}]}");
            CollectionAssert.AreEqual(new[] { "admob" }, router.Resolve(AdsFormat.Rewarded, Placement.NONE).ProviderIds);
        }
    }
}

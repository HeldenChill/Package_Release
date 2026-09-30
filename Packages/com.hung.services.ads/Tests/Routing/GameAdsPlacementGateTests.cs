using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class GameAdsPlacementGateTests
    {
        GameData gameData;
        GameObject go;

        [SetUp]
        public void SetUp()
        {
            gameData = new GameData();
            gameData.InitData(new[] { BaseItemIds.RemoveAds, BaseItemIds.PremiumRemoveAds });
            AdsSessionCounters.Reset();
            AdsSessionCounters.PlayGameAds = 1;
            Locator.Data = new FakeDataService(gameData);
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
            Locator.ResetDataForTests();
        }

        static AdsRoutingData Data(AdsFormat format, Placement placement, string defaultId, string placementId)
        {
            var data = RoutingTestData.With(format, AdsRouteMode.Chain, defaultId);
            data.placements.Add(new AdsPlacementRoute { format = format, placement = placement, mode = AdsRouteMode.Chain, order = new List<string> { placementId } });
            return data;
        }

        [Test]
        public void Reward_DefaultRouteReady_PlacementRouteNot_DoesNotShow()
        {
            var max = new FakeRewarded("max") { Ready = true };
            var admob = new FakeRewarded("admob");
            var set = new AdsProviderSet();
            set.AddRewarded("max", max);
            set.AddRewarded("admob", admob);
            var routed = new RoutedRewardedProvider(new AdsRouter(Data(AdsFormat.Rewarded, Placement.SPIN, "max", "admob"), set.Has), set);
            var registry = new AdsProviderRegistry();
            registry.RegisterRewarded(ADS_TYPE.MAX, routed);

            go = new GameObject("reward");
            var ads = go.AddComponent<GameRewardAds>();
            ads.Type = ADS_TYPE.MAX;
            ads.ConfigureProviders(registry);

            ads.Show(null, null, Placement.SPIN);

            CollectionAssert.IsEmpty(max.Shows);
            CollectionAssert.IsEmpty(admob.Shows);
            Assert.IsFalse(ads.IsShowingAds);
            Assert.AreEqual(0, AdsSessionCounters.WatchedAds);
        }

        [Test]
        public void Reward_PlacementRouteReady_DefaultRouteNot_Shows()
        {
            var max = new FakeRewarded("max");
            var admob = new FakeRewarded("admob") { Ready = true };
            var set = new AdsProviderSet();
            set.AddRewarded("max", max);
            set.AddRewarded("admob", admob);
            var routed = new RoutedRewardedProvider(new AdsRouter(Data(AdsFormat.Rewarded, Placement.SPIN, "max", "admob"), set.Has), set);
            var registry = new AdsProviderRegistry();
            registry.RegisterRewarded(ADS_TYPE.MAX, routed);

            go = new GameObject("reward");
            var ads = go.AddComponent<GameRewardAds>();
            ads.Type = ADS_TYPE.MAX;
            ads.ConfigureProviders(registry);

            ads.Show(null, null, Placement.SPIN);

            CollectionAssert.AreEqual(new[] { Placement.SPIN }, admob.Shows);
            Assert.IsTrue(ads.IsShowingAds);
        }

        [Test]
        public void Inter_DefaultRouteReady_PlacementRouteNot_DoesNotShow()
        {
            var max = new FakeInterstitial { Ready = true };
            var yandex = new FakeInterstitial();
            var set = new AdsProviderSet();
            set.AddInterstitial("max", max);
            set.AddInterstitial("yandex", yandex);
            var routed = new RoutedInterstitialProvider(new AdsRouter(Data(AdsFormat.Interstitial, Placement.IN_GAME, "max", "yandex"), set.Has), set);
            var registry = new AdsProviderRegistry();
            registry.RegisterInterstitial(ADS_TYPE.MAX, routed);

            go = new GameObject("inter");
            var ads = go.AddComponent<GameInterAds>();
            typeof(GameInterAds).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ads, null);
            ads.Type = ADS_TYPE.MAX;
            ads.ConfigureProviders(registry);

            ads.Show(null, Placement.IN_GAME);

            CollectionAssert.IsEmpty(max.Shows);
            CollectionAssert.IsEmpty(yandex.Shows);
            Assert.AreEqual(0, AdsSessionCounters.WatchedAds);
        }

        [Test]
        public void Inter_PlacementRouteReady_DefaultRouteNot_Shows()
        {
            var max = new FakeInterstitial();
            var yandex = new FakeInterstitial { Ready = true };
            var set = new AdsProviderSet();
            set.AddInterstitial("max", max);
            set.AddInterstitial("yandex", yandex);
            var routed = new RoutedInterstitialProvider(new AdsRouter(Data(AdsFormat.Interstitial, Placement.IN_GAME, "max", "yandex"), set.Has), set);
            var registry = new AdsProviderRegistry();
            registry.RegisterInterstitial(ADS_TYPE.MAX, routed);

            go = new GameObject("inter");
            var ads = go.AddComponent<GameInterAds>();
            typeof(GameInterAds).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ads, null);
            ads.Type = ADS_TYPE.MAX;
            ads.ConfigureProviders(registry);

            ads.Show(null, Placement.IN_GAME);

            CollectionAssert.AreEqual(new[] { Placement.IN_GAME }, yandex.Shows);
        }

        sealed class FakeDataService : IDataService
        {
            readonly GameData gameData;
            public FakeDataService(GameData gameData) => this.gameData = gameData;
            public T GetData<T>(int index = 0) where T : class => gameData as T;
            public T GetSOData<T>() where T : ScriptableObject
            {
                var instance = ScriptableObject.CreateInstance<T>();
                typeof(T).GetField("showInterLevelStep", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(instance, 2);
                return instance;
            }
            public T GetUnit<T>(int type) where T : class => null;
            public void Save() { }
        }
    }
}

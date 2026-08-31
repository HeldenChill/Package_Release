using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public class GameInterAdsVendorAgnosticTests
    {
        // AddComponent does not invoke Awake synchronously outside play mode, but
        // GameInterAds.Awake is where cappingTimer/config get assigned - force it here
        // so Show() sees the same state it would after a real domain load.
        private static GameInterAds CreateAwake(GameObject go)
        {
            var interAds = go.AddComponent<GameInterAds>();
            typeof(GameInterAds)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(interAds, null);
            return interAds;
        }

        [SetUp]
        public void SetUp()
        {
            var gameData = new GameData();
            gameData.InitData(new[] { BaseItemIds.RemoveAds, BaseItemIds.PremiumRemoveAds });
            // playGameAdsCount % ShowInterLevelStep must be non-zero or Show() skips
            // via the level-step cap before ever reaching the provider.
            gameData.user.playGameAdsCount = 1;
            Locator.Data = new FakeDataService(gameData);
        }

        [TearDown]
        public void TearDown()
        {
            Locator.ResetDataForTests();
        }

        [Test]
        public void Show_RoutesToProviderRegisteredForActiveType()
        {
            var go = new GameObject(nameof(GameInterAds));
            try
            {
                var interAds = CreateAwake(go);
                var provider = new FakeInterstitialProvider { IsCanShow = true };
                var registry = new AdsProviderRegistry();

                registry.RegisterInterstitial(ADS_TYPE.IRON_SOURCE, provider);
                interAds.Type = ADS_TYPE.IRON_SOURCE;
                interAds.ConfigureProviders(registry);

                interAds.Show(null, Placement.IN_GAME);

                Assert.AreEqual(1, provider.ShowCallCount);
                Assert.AreEqual(Placement.IN_GAME, provider.LastPlacement);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Show_WhenProviderCannotShow_DoesNotCallProvider()
        {
            var go = new GameObject(nameof(GameInterAds));
            try
            {
                var interAds = CreateAwake(go);
                var provider = new FakeInterstitialProvider { IsCanShow = false };
                var registry = new AdsProviderRegistry();

                registry.RegisterInterstitial(ADS_TYPE.MAX, provider);
                interAds.Type = ADS_TYPE.MAX;
                interAds.ConfigureProviders(registry);

                interAds.Show(null, Placement.IN_GAME);

                Assert.AreEqual(0, provider.ShowCallCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private sealed class FakeDataService : IDataService
        {
            private readonly GameData gameData;
            public FakeDataService(GameData gameData) => this.gameData = gameData;
            public T GetData<T>(int index = 0) where T : class => gameData as T;
            public T GetSOData<T>() where T : ScriptableObject
            {
                var instance = ScriptableObject.CreateInstance<T>();
                // showInterLevelStep defaults to 0, and GameInterAds.Show() does
                // playGameAdsCount % ShowInterLevelStep - give it a non-zero step.
                var field = typeof(T).GetField("showInterLevelStep", BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(instance, 2);
                return instance;
            }
            public T GetUnit<T>(int type) where T : class => null;
            public void Save() { }
        }

        private sealed class FakeInterstitialProvider : IInterstitialAdsProvider
        {
            public int ShowCallCount { get; private set; }
            public Placement LastPlacement { get; private set; }

            public bool IsCanShow { get; set; }
            public bool IsLoading => false;
            public void Load() { }

            public void Show(Placement placement)
            {
                ShowCallCount++;
                LastPlacement = placement;
            }

#pragma warning disable 67
            public event System.Action OnAdsLoaded;
            public event System.Action OnAdsLoadFail;
            public event System.Action OnAdsDisplayFail;
            public event System.Action OnAdsDone;
#pragma warning restore 67
        }
    }
}

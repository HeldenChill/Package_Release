using NUnit.Framework;
using UnityEngine;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public class GameBannerAdsVendorAgnosticTests
    {
        [SetUp]
        public void SetUp()
        {
            var gameData = new GameData();
            gameData.InitData(new[] { BaseItemIds.RemoveAds, BaseItemIds.PremiumRemoveAds });
            Locator.Data = new FakeDataService(gameData);
        }

        [TearDown]
        public void TearDown()
        {
            Locator.ResetDataForTests();
        }

        [Test]
        public void ShowWithExplicitType_RoutesToThatVendorsProvider()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var bannerAds = go.AddComponent<GameBannerAds>();
                var maxProvider = new FakeBannerProvider();
                var otherProvider = new FakeBannerProvider();
                var registry = new AdsProviderRegistry();

                registry.RegisterBanner(ADS_TYPE.MAX, maxProvider);
                registry.RegisterBanner(ADS_TYPE.IRON_SOURCE, otherProvider);
                bannerAds.ConfigureProviders(registry);

                bannerAds.Show(ADS_TYPE.IRON_SOURCE);

                Assert.AreEqual(0, maxProvider.ShowCallCount);
                Assert.AreEqual(1, otherProvider.ShowCallCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HideWithUnregisteredType_DoesNotThrow()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var bannerAds = go.AddComponent<GameBannerAds>();
                bannerAds.ConfigureProviders(new AdsProviderRegistry());

                Assert.DoesNotThrow(() => bannerAds.Hide(ADS_TYPE.MAX));
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
            public T GetSOData<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();
            public T GetUnit<T>(int type) where T : class => null;
            public void Save() { }
        }

        private sealed class FakeBannerProvider : IBannerAdsProvider
        {
            public int ShowCallCount { get; private set; }
            public int HideCallCount { get; private set; }

            public void InitBanner() { }
            public void Show() { ShowCallCount++; }
            public void Hide() { HideCallCount++; }
            public void Destroy() { }
            public void Load() { }

#pragma warning disable 67
            public event System.Action OnAdsLoaded;
            public event System.Action OnAdsLoadFail;
#pragma warning restore 67
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public class GameRewardAdsVendorAgnosticTests
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
        public void Show_RoutesToProviderRegisteredForActiveType()
        {
            var go = new GameObject(nameof(GameRewardAds));
            try
            {
                var rewardAds = go.AddComponent<GameRewardAds>();
                var provider = new FakeRewardedProvider();
                var registry = new AdsProviderRegistry();

                // IRON_SOURCE stands in for "some vendor the core does not name".
                registry.RegisterRewarded(ADS_TYPE.IRON_SOURCE, provider);
                rewardAds.Type = ADS_TYPE.IRON_SOURCE;
                rewardAds.ConfigureProviders(registry);

                rewardAds.Show(null, null, Placement.X2_COIN);

                Assert.AreEqual(1, provider.ShowCallCount);
                Assert.AreEqual(Placement.X2_COIN, provider.LastPlacement);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Show_WithNoProviderForActiveType_DoesNotThrow()
        {
            var go = new GameObject(nameof(GameRewardAds));
            try
            {
                var rewardAds = go.AddComponent<GameRewardAds>();
                rewardAds.Type = ADS_TYPE.MAX;
                rewardAds.ConfigureProviders(new AdsProviderRegistry());

                Assert.DoesNotThrow(() => rewardAds.Show(null, null, Placement.NONE));
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
            public T GetSOData<T>() where T : ScriptableObject => null;
            public T GetUnit<T>(int type) where T : class => null;
            public void Save() { }
        }

        private sealed class FakeRewardedProvider : IRewardedAdsProvider
        {
            public int ShowCallCount { get; private set; }
            public Placement LastPlacement { get; private set; }

            public bool IsCanShow => true;
            public bool IsLoading => false;
            public void Load() { }

            public void Show(Placement placement = Placement.NONE)
            {
                ShowCallCount++;
                LastPlacement = placement;
            }

#pragma warning disable 67
            public event System.Action OnAdsLoaded;
            public event System.Action OnAdsLoadFail;
            public event System.Action OnAdsDisplayFail;
            public event System.Action OnAdsReceiveReward;
            public event System.Action OnAdsHidden;
#pragma warning restore 67
        }
    }
}

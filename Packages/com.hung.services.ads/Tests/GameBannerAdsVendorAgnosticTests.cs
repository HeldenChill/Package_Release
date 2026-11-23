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
            Hung.Base.ServiceSlots.Unregister<IDataService>(Hung.Base.ServiceSlots.Get<IDataService>()); Hung.Base.ServiceSlots.Register<IDataService>(new FakeDataService(gameData));
        }

        [TearDown]
        public void TearDown()
        {
            Locator.ResetDataForTests();
            Hung.Base.ServiceSlots.Unregister<IAdsEntitlements>(Hung.Base.ServiceSlots.Get<IAdsEntitlements>());
        }

        [Test]
        public void ShowWithExplicitType_HostRemoveAds_DoesNotShow()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var bannerAds = go.AddComponent<GameBannerAds>();
                var maxProvider = new FakeBannerProvider();
                var registry = new AdsProviderRegistry();
                registry.RegisterBanner(ADS_TYPE.MAX, maxProvider);
                bannerAds.ConfigureProviders(registry);
                Hung.Base.ServiceSlots.Unregister<IAdsEntitlements>(Hung.Base.ServiceSlots.Get<IAdsEntitlements>()); Hung.Base.ServiceSlots.Register<IAdsEntitlements>(new FakeEntitlements { IsRemoveAds = true });

                bannerAds.Show(ADS_TYPE.MAX);

                Assert.AreEqual(0, maxProvider.ShowCallCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private sealed class FakeEntitlements : IAdsEntitlements
        {
            public bool IsRemoveAds { get; set; }
            public bool IsPremiumRemoveAds { get; set; }
            public int LevelIndex { get; set; }
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
            public void RaiseLoaded() => OnAdsLoaded?.Invoke();

#pragma warning disable 67
            public event System.Action OnAdsLoaded;
            public event System.Action OnAdsLoadFail;
#pragma warning restore 67
        }

        // BUG-261114 A1 banner: MAX fires its loaded event on every auto-refresh. The loaded
        // handler used to call Show unconditionally, so a banner the game hid came back.
        private GameBannerAds NewBanner(GameObject go, FakeBannerProvider provider)
        {
            var bannerAds = go.AddComponent<GameBannerAds>();
            var registry = new AdsProviderRegistry();
            registry.RegisterBanner(ADS_TYPE.MAX, provider);
            bannerAds.ConfigureProviders(registry);
            bannerAds.Type = ADS_TYPE.MAX;
            return bannerAds;
        }

        [Test]
        public void HiddenBanner_RefreshLoad_DoesNotShowAgain()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var provider = new FakeBannerProvider();
                GameBannerAds bannerAds = NewBanner(go, provider);
                bannerAds.Show();
                bannerAds.Hide();

                provider.RaiseLoaded();

                Assert.AreEqual(1, provider.ShowCallCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InitBanner_FirstLoad_Shows()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var provider = new FakeBannerProvider();
                GameBannerAds bannerAds = NewBanner(go, provider);
                bannerAds.InitBanner();

                provider.RaiseLoaded();

                Assert.AreEqual(1, provider.ShowCallCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // A banner already on screen when remove-ads is granted must be hidden, not left up.
        [Test]
        public void RemoveAds_RefreshLoad_HidesInsteadOfShowing()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var provider = new FakeBannerProvider();
                GameBannerAds bannerAds = NewBanner(go, provider);
                bannerAds.Show();
                Hung.Base.ServiceSlots.Unregister<IAdsEntitlements>(Hung.Base.ServiceSlots.Get<IAdsEntitlements>()); Hung.Base.ServiceSlots.Register<IAdsEntitlements>(new FakeEntitlements { IsRemoveAds = true });

                provider.RaiseLoaded();

                Assert.AreEqual(1, provider.ShowCallCount);
                Assert.GreaterOrEqual(provider.HideCallCount, 1);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Show_WithRemoveAds_HidesVisibleBanner()
        {
            var go = new GameObject(nameof(GameBannerAds));
            try
            {
                var provider = new FakeBannerProvider();
                GameBannerAds bannerAds = NewBanner(go, provider);
                Hung.Base.ServiceSlots.Unregister<IAdsEntitlements>(Hung.Base.ServiceSlots.Get<IAdsEntitlements>()); Hung.Base.ServiceSlots.Register<IAdsEntitlements>(new FakeEntitlements { IsRemoveAds = true });

                bannerAds.Show();

                Assert.AreEqual(0, provider.ShowCallCount);
                Assert.AreEqual(1, provider.HideCallCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

using NUnit.Framework;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public class AdsProviderRegistryTests
    {
        [Test]
        public void TryGetRewarded_ReturnsRegisteredProvider()
        {
            var registry = new AdsProviderRegistry();
            var provider = new FakeRewardedProvider();

            registry.RegisterRewarded(ADS_TYPE.MAX, provider);

            Assert.IsTrue(registry.TryGetRewarded(ADS_TYPE.MAX, out var found));
            Assert.AreSame(provider, found);
        }

        [Test]
        public void TryGetRewarded_UnregisteredType_ReturnsFalseAndNull()
        {
            var registry = new AdsProviderRegistry();

            Assert.IsFalse(registry.TryGetRewarded(ADS_TYPE.IRON_SOURCE, out var found));
            Assert.IsNull(found);
        }

        [Test]
        public void RegisterRewarded_SameTypeTwice_LastWins()
        {
            var registry = new AdsProviderRegistry();
            var first = new FakeRewardedProvider();
            var second = new FakeRewardedProvider();

            registry.RegisterRewarded(ADS_TYPE.MAX, first);
            registry.RegisterRewarded(ADS_TYPE.MAX, second);

            registry.TryGetRewarded(ADS_TYPE.MAX, out var found);
            Assert.AreSame(second, found);
        }

        [Test]
        public void RegisterRewarded_NullProvider_IsIgnored()
        {
            var registry = new AdsProviderRegistry();

            registry.RegisterRewarded(ADS_TYPE.MAX, null);

            Assert.IsFalse(registry.TryGetRewarded(ADS_TYPE.MAX, out _));
        }

        private sealed class FakeRewardedProvider : IRewardedAdsProvider
        {
            public bool IsCanShow => true;
            public bool IsLoading => false;
            public void Load() { }
            public void Show(Placement placement = Placement.NONE) { }
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

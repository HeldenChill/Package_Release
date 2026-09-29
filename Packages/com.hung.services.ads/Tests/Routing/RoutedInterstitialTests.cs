using NUnit.Framework;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class RoutedInterstitialTests
    {
        FakeInterstitial max, yandex;
        int loaded, failed, done, displayFails;

        RoutedInterstitialProvider Build(AdsRouteMode mode, params string[] order)
        {
            max = new FakeInterstitial();
            yandex = new FakeInterstitial();
            var set = new AdsProviderSet();
            set.AddInterstitial("max", max);
            set.AddInterstitial("yandex", yandex);
            var r = new RoutedInterstitialProvider(new AdsRouter(RoutingTestData.With(AdsFormat.Interstitial, mode, order), set.Has), set);
            loaded = failed = done = displayFails = 0;
            r.OnAdsLoaded += () => loaded++;
            r.OnAdsLoadFail += () => failed++;
            r.OnAdsDone += () => done++;
            r.OnAdsDisplayFail += () => displayFails++;
            return r;
        }

        [Test]
        public void Chain_FailThenLoad_LoadedOnce()
        {
            var r = Build(AdsRouteMode.Chain, "yandex", "max");
            r.Load();
            yandex.RaiseLoadFail();
            max.RaiseLoaded();
            Assert.AreEqual(1, loaded);
            Assert.AreEqual(0, failed);
        }

        [Test]
        public void Parallel_AllFail_FailOnce()
        {
            var r = Build(AdsRouteMode.Parallel, "yandex", "max");
            r.Load();
            yandex.RaiseLoadFail();
            max.RaiseLoadFail();
            Assert.AreEqual(1, failed);
        }

        [Test]
        public void Done_OnlyFromShowingChild_Once()
        {
            var r = Build(AdsRouteMode.Parallel, "yandex", "max");
            yandex.Ready = true;
            max.Ready = true;
            r.Show(Placement.IN_GAME);
            max.RaiseDone();
            yandex.RaiseDone();
            yandex.RaiseDone();
            Assert.AreEqual(1, done);
            Assert.AreEqual(1, yandex.Shows.Count);
        }

        [Test]
        public void Show_NothingReady_DisplayFail()
        {
            var r = Build(AdsRouteMode.Chain, "max");
            r.Show(Placement.IN_GAME);
            Assert.AreEqual(1, displayFails);
        }
    }
}

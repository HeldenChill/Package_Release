using NUnit.Framework;
using Hung.Ads;

namespace Hung.Ads.Tests.Routing
{
    public class RoutedBannerTests
    {
        FakeBanner admob, max;
        int loaded, failed;

        RoutedBannerProvider Build(params string[] order)
        {
            admob = new FakeBanner();
            max = new FakeBanner();
            var set = new AdsProviderSet();
            set.AddBanner("admob", admob);
            set.AddBanner("max", max);
            var r = new RoutedBannerProvider(new AdsRouter(RoutingTestData.With(AdsFormat.Banner, AdsRouteMode.Chain, order), set.Has), set);
            loaded = failed = 0;
            r.OnAdsLoaded += () => loaded++;
            r.OnAdsLoadFail += () => failed++;
            return r;
        }

        [Test]
        public void Load_InitsAndLoadsFirstOnly()
        {
            var r = Build("admob", "max");
            r.Load();
            CollectionAssert.AreEqual(new[] { "init", "load" }, admob.Calls);
            CollectionAssert.IsEmpty(max.Calls);
        }

        [Test]
        public void FirstFails_DestroyedThenNextInitLoaded()
        {
            var r = Build("admob", "max");
            r.Load();
            admob.RaiseLoadFail();
            CollectionAssert.AreEqual(new[] { "init", "load", "destroy" }, admob.Calls);
            CollectionAssert.AreEqual(new[] { "init", "load" }, max.Calls);
            max.RaiseLoaded();
            Assert.AreEqual(1, loaded);
            r.Show();
            Assert.AreEqual("show", max.Calls[max.Calls.Count - 1]);
        }

        [Test]
        public void AllFail_FailOnce_NextLoadRestartsAtHead()
        {
            var r = Build("admob", "max");
            r.Load();
            admob.RaiseLoadFail();
            max.RaiseLoadFail();
            Assert.AreEqual(1, failed);
            admob.Calls.Clear();
            r.Load();
            CollectionAssert.AreEqual(new[] { "init", "load" }, admob.Calls);
        }

        [Test]
        public void EventsFromNonCurrentChild_Ignored()
        {
            var r = Build("admob", "max");
            r.Load();
            max.RaiseLoaded();
            max.RaiseLoadFail();
            Assert.AreEqual(0, loaded);
            Assert.AreEqual(0, failed);
        }

        [Test]
        public void EmptyRoute_LoadFailsOnce()
        {
            var r = Build();
            r.Load();
            Assert.AreEqual(1, failed);
        }
    }
}

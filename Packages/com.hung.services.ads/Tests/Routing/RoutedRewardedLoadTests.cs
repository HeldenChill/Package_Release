using System.Collections.Generic;
using NUnit.Framework;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class RoutedRewardedLoadTests
    {
        FakeRewarded max, admob, yandex;
        int loaded, failed;

        RoutedRewardedProvider Build(AdsRoutingData data)
        {
            max = new FakeRewarded("max");
            admob = new FakeRewarded("admob");
            yandex = new FakeRewarded("yandex");
            var set = new AdsProviderSet();
            set.AddRewarded("max", max);
            set.AddRewarded("admob", admob);
            set.AddRewarded("yandex", yandex);
            var routed = new RoutedRewardedProvider(new AdsRouter(data, set.Has), set);
            loaded = failed = 0;
            routed.OnAdsLoaded += () => loaded++;
            routed.OnAdsLoadFail += () => failed++;
            return routed;
        }

        [Test]
        public void Chain_LoadsOnlyFirst()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max", "admob"));
            r.Load();
            Assert.AreEqual(1, max.LoadCalls);
            Assert.AreEqual(0, admob.LoadCalls);
            Assert.IsTrue(r.IsLoading);
        }

        [Test]
        public void Chain_FirstFails_LoadsNext_ThenLoadedOnce()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max", "admob"));
            r.Load();
            max.RaiseLoadFail();
            Assert.AreEqual(1, admob.LoadCalls);
            Assert.AreEqual(0, failed);
            admob.RaiseLoaded();
            Assert.AreEqual(1, loaded);
            Assert.IsFalse(r.IsLoading);
            Assert.IsTrue(r.IsCanShow);
        }

        [Test]
        public void Chain_AllFail_RaisesFailOnce()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max", "admob"));
            r.Load();
            max.RaiseLoadFail();
            admob.RaiseLoadFail();
            Assert.AreEqual(1, failed);
            Assert.AreEqual(0, loaded);
            Assert.IsFalse(r.IsLoading);
        }

        [Test]
        public void Chain_FirstAlreadyReady_LoadedWithoutLoadCall()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max", "admob"));
            max.Ready = true;
            r.Load();
            Assert.AreEqual(0, max.LoadCalls);
            Assert.AreEqual(1, loaded);
        }

        [Test]
        public void Parallel_LoadsAll_FirstReadyRaisesLoadedOnce()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob", "yandex"));
            r.Load();
            Assert.AreEqual(1, max.LoadCalls);
            Assert.AreEqual(1, admob.LoadCalls);
            Assert.AreEqual(1, yandex.LoadCalls);
            admob.RaiseLoaded();
            max.RaiseLoaded();
            Assert.AreEqual(1, loaded);
        }

        [Test]
        public void Parallel_AllFail_RaisesFailOnce_DuplicateFailIgnored()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob"));
            r.Load();
            max.RaiseLoadFail();
            max.RaiseLoadFail();
            Assert.AreEqual(0, failed);
            admob.RaiseLoadFail();
            Assert.AreEqual(1, failed);
        }

        [Test]
        public void EmptyRoute_LoadReportsFailOnce()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain));
            r.Load();
            Assert.AreEqual(1, failed);
            Assert.IsFalse(r.IsCanShow);
            Assert.IsFalse(r.IsLoading);
        }

        [Test]
        public void Load_WhileRunning_Ignored()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            r.Load();
            r.Load();
            Assert.AreEqual(1, max.LoadCalls);
        }

        [Test]
        public void LoadFail_HandlerReloadsSynchronously_NoThrow()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob"));
            int reloads = 0;
            r.OnAdsLoadFail += () => { if (reloads++ == 0) r.Load(); };
            r.Load();
            max.RaiseLoadFail();
            Assert.DoesNotThrow(() => admob.RaiseLoadFail());
            Assert.AreEqual(2, max.LoadCalls, "reload started a new cycle");
            Assert.IsTrue(r.IsLoading);
        }

        [Test]
        public void SynchronousChildResults_ChainAdvancesInsideLoad()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max", "admob"));
            max.AutoLoadResult = false;
            admob.AutoLoadResult = true;
            r.Load();
            Assert.AreEqual(1, loaded);
            Assert.AreEqual(0, failed);
        }

        [Test]
        public void PlacementRoute_PreloadedSilently()
        {
            var data = RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max");
            data.placements.Add(new AdsPlacementRoute { format = AdsFormat.Rewarded, placement = Placement.REROLL_SKILL_CARD, mode = AdsRouteMode.Chain, order = new List<string> { "yandex" } });
            var r = Build(data);
            r.Load();
            Assert.AreEqual(1, yandex.LoadCalls);
            yandex.RaiseLoadFail();
            Assert.AreEqual(0, failed, "placement route failure is silent");
            max.RaiseLoaded();
            Assert.AreEqual(1, loaded);
        }
    }
}

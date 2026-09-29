using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Analytics.Tests
{
    public class AnalyticsServiceTests
    {
        static (AnalyticsService service, RecordingAnalyticsBackend ads, RecordingAnalyticsBackend product) TwoBackends()
        {
            var service = new AnalyticsService(() => 0f);
            var ads = new RecordingAnalyticsBackend("ads");
            var product = new RecordingAnalyticsBackend("product");
            service.AddBackend(ads, AnalyticsCategory.Ads);
            service.AddBackend(product, AnalyticsCategory.Product | AnalyticsCategory.Revenue);
            return (service, ads, product);
        }

        [Test]
        public void LogEvent_ReachesOnlyBackendsSubscribedToItsCategory()
        {
            var (service, ads, product) = TwoBackends();

            service.LogEvent("custom_gd_event", AnalyticsCategory.Product, new Dictionary<string, object> { { "k", 1 } });

            Assert.AreEqual(0, ads.Events.Count);
            Assert.AreEqual(1, product.Events.Count);
            Assert.AreEqual("custom_gd_event", product.Events[0].Name);
            Assert.AreEqual(1, product.Events[0].Parameters["k"]);
        }

        [Test]
        public void LogEvent_CategoryNone_ReachesNobody()
        {
            var (service, ads, product) = TwoBackends();

            service.LogEvent("x", AnalyticsCategory.None);

            Assert.AreEqual(0, ads.Events.Count + product.Events.Count);
        }

        [Test]
        public void BackendThatThrows_DoesNotStopOthers_AndIsReportedOnce()
        {
            var service = new AnalyticsService(() => 0f);
            var broken = new RecordingAnalyticsBackend("broken") { ThrowOnLog = new InvalidOperationException("boom") };
            var healthy = new RecordingAnalyticsBackend("healthy");
            service.AddBackend(broken, AnalyticsCategory.All);
            service.AddBackend(healthy, AnalyticsCategory.All);

            LogAssert.Expect(LogType.Error, new Regex("backend 'broken' threw InvalidOperationException"));
            Assert.DoesNotThrow(() => service.LogEvent("a", AnalyticsCategory.Design));
            Assert.DoesNotThrow(() => service.LogEvent("b", AnalyticsCategory.Design));

            Assert.AreEqual(2, healthy.Events.Count);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BackendThatThrows_FromManyThreads_NeverThrowsAndReportsOnce()
        {
            // LevelPlay raises revenue callbacks on a background thread; failure bookkeeping must be thread-safe.
            var service = new AnalyticsService(() => 0f);
            service.AddBackend(new RecordingAnalyticsBackend("broken") { ThrowOnLog = new InvalidOperationException("boom") }, AnalyticsCategory.All);

            LogAssert.Expect(LogType.Error, new Regex("backend 'broken' threw InvalidOperationException"));
            var tasks = new System.Threading.Tasks.Task[8];
            for (int t = 0; t < tasks.Length; t++)
                tasks[t] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int i = 0; i < 200; i++) service.LogEvent("a", AnalyticsCategory.Design);
                });

            Assert.DoesNotThrow(() => System.Threading.Tasks.Task.WaitAll(tasks));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ZeroBackends_EveryMemberIsANoOp()
        {
            var service = new AnalyticsService(() => 0f);

            Assert.DoesNotThrow(() =>
            {
                service.LogEvent("x", AnalyticsCategory.All);
                service.SetUserProperty("k", "v");
                service.AdsRewardOffer(Placement.NONE);
                service.AdsRewardClick(Placement.NONE);
                service.AdsRewardShow(Placement.NONE);
                service.AdsRewardShowFail(Placement.NONE, "e");
                service.AdsRewardComplete(Placement.NONE, "t");
                service.AdsRewardLoadComplete();
                service.AdsRewardLoad();
                service.AdsRewardLoadFail();
                service.AdsInterFail("e");
                service.AdsInterLoad();
                service.AdsInterShow(Placement.NONE);
                service.AdsInterClick();
                service.AdsInterLoadComplete();
                service.AdsInterComplete();
                service.BuyIAPComplete(IAP_ITEM.NONE, Placement.NONE);
                service.TutorialStep("tut", 1);
                service.LevelTrackEvent(LEVEL_STATE.COMPLETE, 1, 3, true, false);
                service.EarnVirtualCurrency("gold", 5, "win");
                service.SpendVirtualCurrency("gold", 5, "shop");
                service.OnRevenue("net", 0.01, "USD");
            });
        }

        [Test]
        public void OnRevenue_ReachesOnlyRevenueSubscribers_WithNeutralExtra()
        {
            var (service, ads, product) = TwoBackends();
            var extra = new Dictionary<string, string> { { "mediation", "ironsource" }, { "country", "VN" } };

            service.OnRevenue("unityads", 0.02, "USD", extra);

            Assert.AreEqual(0, ads.Revenues.Count);
            Assert.AreEqual(1, product.Revenues.Count);
            Assert.AreEqual("unityads", product.Revenues[0].Source);
            Assert.AreEqual(0.02, product.Revenues[0].Value);
            Assert.AreEqual("ironsource", product.Revenues[0].Extra["mediation"]);
        }

        [Test]
        public void SetUserProperty_ReachesEveryBackendRegardlessOfCategory()
        {
            var (service, ads, product) = TwoBackends();

            service.SetUserProperty("level", "7");

            Assert.AreEqual("7", ads.UserProperties["level"]);
            Assert.AreEqual("7", product.UserProperties["level"]);
        }

        [TestCase(LEVEL_STATE.START, false, false, new[] { "level_4_start" })]
        [TestCase(LEVEL_STATE.START, false, true, new[] { "ftu_level_4_start" })]
        [TestCase(LEVEL_STATE.COMPLETE, true, false, new[] { "level_4_stars_3", "level_4_pass" })]
        [TestCase(LEVEL_STATE.COMPLETE, false, false, new[] { "level_4_stars_3", "level_4_replay_pass" })]
        [TestCase(LEVEL_STATE.COMPLETE, true, true, new[] { "ftu_level_4_star_3", "ftu_level_4_pass" })]
        [TestCase(LEVEL_STATE.COMPLETE, false, true, new[] { "ftu_level_4_star_3", "ftu_level_4_replay_pass" })]
        [TestCase(LEVEL_STATE.FAIL, false, false, new[] { "level_4_fail" })]
        [TestCase(LEVEL_STATE.FAIL, false, true, new[] { "ftu_level_4_fail" })]
        public void LevelTrackEvent_EmitsDashboardNames(LEVEL_STATE state, bool firstPass, bool isFtu, string[] expected)
        {
            var service = new AnalyticsService(() => 0f);
            var all = new RecordingAnalyticsBackend();
            service.AddBackend(all, AnalyticsCategory.All);

            service.LevelTrackEvent(state, 4, 3, firstPass, isFtu);

            CollectionAssert.AreEqual(expected, all.Events.ConvertAll(e => e.Name));
        }

        [Test]
        public void LevelTrackEvent_PassGoesToProduct_StarsStayInDesign()
        {
            var service = new AnalyticsService(() => 0f);
            var product = new RecordingAnalyticsBackend();
            service.AddBackend(product, AnalyticsCategory.Product);

            service.LevelTrackEvent(LEVEL_STATE.COMPLETE, 2, 1, true, false);

            CollectionAssert.AreEqual(new[] { "level_2_pass" }, product.Events.ConvertAll(e => e.Name));
        }

        [Test]
        public void FirstAdsSession_FiresOnceWithWholeSecondsSinceStart()
        {
            float now = 10f;
            var service = new AnalyticsService(() => now);
            var backend = new RecordingAnalyticsBackend();
            service.AddBackend(backend, AnalyticsCategory.Ads);
            now = 42.7f;

            service.AdsInterShow(Placement.IN_GAME);
            service.AdsRewardShow(Placement.IN_GAME);

            var first = backend.Events.FindAll(e => e.Name == "first_ads_session");
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(32, first[0].Parameters["duration"]);
            Assert.AreEqual("IN_GAME", first[0].Parameters["placement"]);
        }
    }
}

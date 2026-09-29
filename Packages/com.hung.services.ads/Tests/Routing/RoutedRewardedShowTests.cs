using System.Collections.Generic;
using NUnit.Framework;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class RoutedRewardedShowTests
    {
        FakeRewarded max, admob, yandex;
        int rewards, hidden, displayFails;

        RoutedRewardedProvider Build(AdsRoutingData data)
        {
            max = new FakeRewarded("max");
            admob = new FakeRewarded("admob");
            yandex = new FakeRewarded("yandex");
            var set = new AdsProviderSet();
            set.AddRewarded("max", max);
            set.AddRewarded("admob", admob);
            set.AddRewarded("yandex", yandex);
            var r = new RoutedRewardedProvider(new AdsRouter(data, set.Has), set);
            rewards = hidden = displayFails = 0;
            r.OnAdsReceiveReward += () => rewards++;
            r.OnAdsHidden += () => hidden++;
            r.OnAdsDisplayFail += () => displayFails++;
            return r;
        }

        [Test]
        public void Show_PicksFirstReadyInRouteOrder_NotArrivalOrder()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob"));
            admob.Ready = true;
            max.Ready = true;
            r.Show(Placement.X2_COIN);
            CollectionAssert.AreEqual(new[] { Placement.X2_COIN }, max.Shows);
            CollectionAssert.IsEmpty(admob.Shows);
        }

        [Test]
        public void Show_UsesPlacementRoute()
        {
            var data = RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max");
            data.placements.Add(new AdsPlacementRoute { format = AdsFormat.Rewarded, placement = Placement.REROLL_SKILL_CARD, mode = AdsRouteMode.Chain, order = new List<string> { "yandex" } });
            var r = Build(data);
            max.Ready = true;
            Assert.IsFalse(r.IsCanShowFor(Placement.REROLL_SKILL_CARD));
            yandex.Ready = true;
            Assert.IsTrue(r.IsCanShowFor(Placement.REROLL_SKILL_CARD));
            r.Show(Placement.REROLL_SKILL_CARD);
            Assert.AreEqual(1, yandex.Shows.Count);
            CollectionAssert.IsEmpty(max.Shows);
        }

        [Test]
        public void Show_NothingReady_DisplayFailOnce_NoChildShown()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            r.Show();
            Assert.AreEqual(1, displayFails);
            CollectionAssert.IsEmpty(max.Shows);
        }

        [Test]
        public void Reward_FromShowingChild_Forwarded()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            max.Ready = true;
            r.Show();
            max.RaiseReward();
            max.RaiseHidden();
            Assert.AreEqual(1, rewards);
            Assert.AreEqual(1, hidden);
        }

        [Test]
        public void Reward_FromOtherChild_Dropped()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob"));
            max.Ready = true;
            r.Show();
            admob.RaiseReward();
            admob.RaiseHidden();
            Assert.AreEqual(0, rewards);
            Assert.AreEqual(0, hidden);
        }

        [Test]
        public void Reward_Twice_ForwardedOnce()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            max.Ready = true;
            r.Show();
            max.RaiseReward();
            max.RaiseReward();
            Assert.AreEqual(1, rewards);
        }

        [Test]
        public void Reward_AfterHidden_StillForwardedOnce()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            max.Ready = true;
            r.Show();
            max.RaiseHidden();
            max.RaiseReward();
            Assert.AreEqual(1, hidden);
            Assert.AreEqual(1, rewards);
        }

        [Test]
        public void Reward_BeforeAnyShow_Dropped()
        {
            Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            max.RaiseReward();
            Assert.AreEqual(0, rewards);
        }

        [Test]
        public void DisplayFail_FromShowingChild_ForwardedOnce_NoFallbackShow()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob"));
            max.Ready = true;
            admob.Ready = true;
            r.Show();
            max.RaiseDisplayFail();
            max.RaiseDisplayFail();
            Assert.AreEqual(1, displayFails);
            CollectionAssert.IsEmpty(admob.Shows, "no mid-show fallback");
        }

        [Test]
        public void Show_WhileShowing_Ignored()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Parallel, "max", "admob"));
            max.Ready = true;
            admob.Ready = true;
            r.Show();
            r.Show();
            Assert.AreEqual(1, max.Shows.Count);
            CollectionAssert.IsEmpty(admob.Shows);
        }

        [Test]
        public void Dispose_StopsForwarding()
        {
            var r = Build(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"));
            max.Ready = true;
            r.Show();
            r.Dispose();
            max.RaiseReward();
            Assert.AreEqual(0, rewards);
        }
    }
}

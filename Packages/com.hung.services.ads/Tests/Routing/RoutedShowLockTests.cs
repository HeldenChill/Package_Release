using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class RoutedShowLockTests
    {
        float now;

        [SetUp] public void SetUp() => now = 0f;

        [Test]
        public void Rewarded_LostHiddenEvent_LockExpiresAfterTimeout()
        {
            var max = new FakeRewarded("max") { Ready = true };
            var set = new AdsProviderSet();
            set.AddRewarded("max", max);
            var r = new RoutedRewardedProvider(new AdsRouter(RoutingTestData.With(AdsFormat.Rewarded, AdsRouteMode.Chain, "max"), set.Has), set, () => now, 100f);

            r.Show();
            Assert.AreEqual(1, max.Shows.Count);

            now = 99f;
            max.Ready = true;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("already showing"));
            r.Show();
            Assert.AreEqual(1, max.Shows.Count, "still locked before timeout");

            now = 101f;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("lock expired"));
            r.Show();
            Assert.AreEqual(2, max.Shows.Count, "lock expired, new show allowed");
        }

        [Test]
        public void Interstitial_LostDoneEvent_LockExpiresAfterTimeout()
        {
            var max = new FakeInterstitial { Ready = true };
            var set = new AdsProviderSet();
            set.AddInterstitial("max", max);
            var r = new RoutedInterstitialProvider(new AdsRouter(RoutingTestData.With(AdsFormat.Interstitial, AdsRouteMode.Chain, "max"), set.Has), set, () => now, 100f);

            r.Show(Placement.IN_GAME);
            Assert.AreEqual(1, max.Shows.Count);

            now = 99f;
            max.Ready = true;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("already showing"));
            r.Show(Placement.IN_GAME);
            Assert.AreEqual(1, max.Shows.Count, "still locked before timeout");

            now = 101f;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("lock expired"));
            r.Show(Placement.IN_GAME);
            Assert.AreEqual(2, max.Shows.Count, "lock expired, new show allowed");
        }
    }
}

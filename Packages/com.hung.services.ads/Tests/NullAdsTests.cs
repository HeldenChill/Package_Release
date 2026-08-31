using NUnit.Framework;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests
{
    // NullAdsService (Runtime/Doubles/NullAdsService.cs, Ph6 test double). Plan assumed an IsReady
    // property to assert false - IAdsService/IAds have no IsReady member at all (checked
    // Hung.Base's IAdsService.cs); characterized what actually exists instead: every Show/Hide/Load
    // is a true no-op, EXCEPT Reward/Inter's callback-Show overloads, which invoke their callbacks
    // synchronously and immediately (real gotcha - a caller awaiting an async ad flow gets it
    // resolved on the same frame against this double).
    public class NullAdsTests
    {
        [Test]
        public void NullAds_AppOpenAndBanner_AllCallsNoThrow()
        {
            var service = new NullAdsService();

            Assert.DoesNotThrow(() =>
            {
                service.AppOpen.Load();
                service.AppOpen.Show();
                service.AppOpen.Hide();
                service.Banner.Load();
                service.Banner.Show();
                service.Banner.Hide();
            });
        }

        [Test]
        public void NullRewardAds_Show_InvokesOnlyHiddenCallbackImmediately()
        {
            var service = new NullAdsService();
            bool rewarded = false;
            bool hidden = false;

            service.Reward.Show(() => rewarded = true, () => hidden = true);

            Assert.IsFalse(rewarded);
            Assert.IsTrue(hidden);
        }

        [Test]
        public void NullAdsService_RequestApis_ReportUnsupported()
        {
            var service = new NullAdsService();
            AdsShowResult reward = default;
            AdsShowResult inter = default;

            service.ShowRewarded(new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "1")), result => reward = result);
            service.ShowInterstitial(new AdsShowRequest(AdsRequestId.Create("inter", AdsRequestKind.Interstitial, Placement.IN_GAME, "1")), result => inter = result);

            Assert.AreEqual(AdsRequestOutcome.Unsupported, reward.Outcome);
            Assert.AreEqual("null-service", reward.DiagnosticCode);
            Assert.AreEqual(AdsRequestOutcome.Unsupported, inter.Outcome);
            Assert.AreEqual("null-service", inter.DiagnosticCode);
        }

        [Test]
        public void NullInterAds_Show_InvokesCallbackImmediately()
        {
            var service = new NullAdsService();
            bool shown = false;

            service.Inter.Show(() => shown = true, Placement.NONE);

            Assert.IsTrue(shown);
        }
    }
}

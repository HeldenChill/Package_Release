using NUnit.Framework;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public sealed class GameInterAdsRequestTests
    {
        [Test]
        public void Done_InvokesContinuationOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("inter", AdsRequestKind.Interstitial, Placement.IN_GAME, "1"));
            int completions = 0;
            controller.TryBegin(request, _ => completions++, out var context, out _);
            var session = new InterstitialRequestSession(context);

            session.OnDone();
            session.OnDone();

            Assert.AreEqual(1, completions);
        }

        [Test]
        public void Unavailable_ContinuesFlowOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("inter", AdsRequestKind.Interstitial, Placement.IN_GAME, "2"));
            AdsShowResult observed = default;
            controller.TryBegin(request, result => observed = result, out var context, out _);

            new InterstitialRequestSession(context).OnUnavailable();

            Assert.AreEqual(AdsRequestOutcome.Unavailable, observed.Outcome);
            Assert.IsTrue(observed.ShouldContinueFlow);
        }
    }
}

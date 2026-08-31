using NUnit.Framework;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public sealed class GameRewardAdsRequestTests
    {
        [Test]
        public void HiddenAfterReward_CompletesEarnedRewardOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "1"));
            AdsShowResult observed = default;
            controller.TryBegin(request, result => observed = result, out var context, out _);
            var session = new RewardedRequestSession(context);

            session.OnRewardEarned("provider-reward");
            session.OnHidden();
            session.OnDisplayFailed();

            Assert.AreEqual(AdsRequestOutcome.Completed, observed.Outcome);
            Assert.IsTrue(observed.IsEarnedReward);
            Assert.AreEqual("provider-reward", observed.ProviderEvidence);
        }

        [Test]
        public void HiddenWithoutReward_CompletesSkippedOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "2"));
            AdsShowResult observed = default;
            controller.TryBegin(request, result => observed = result, out var context, out _);

            new RewardedRequestSession(context).OnHidden();

            Assert.AreEqual(AdsRequestOutcome.Skipped, observed.Outcome);
            Assert.IsFalse(observed.IsEarnedReward);
            Assert.IsTrue(observed.ShouldContinueFlow);
        }
    }
}

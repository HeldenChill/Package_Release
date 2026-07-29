using NUnit.Framework;
using Hung.Base;

namespace Hung.Base.Tests
{
    public sealed class AdsRequestContractTests
    {
        [Test]
        public void AdsRequestId_Create_IsStableAndPlacementScoped()
        {
            var a = AdsRequestId.Create("daily-reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "request-1");
            var replay = AdsRequestId.Create("daily-reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "request-1");
            var otherPlacement = AdsRequestId.Create("daily-reward", AdsRequestKind.Rewarded, Placement.SPIN, "request-1");

            Assert.AreEqual(a, replay);
            Assert.AreNotEqual(a, otherPlacement);
            Assert.IsFalse(string.IsNullOrWhiteSpace(a.Value));
        }

        [TestCase(AdsRequestOutcome.Completed, true, true)]
        [TestCase(AdsRequestOutcome.Skipped, false, true)]
        [TestCase(AdsRequestOutcome.Unavailable, false, true)]
        [TestCase(AdsRequestOutcome.Unsupported, false, true)]
        [TestCase(AdsRequestOutcome.Misconfigured, false, false)]
        [TestCase(AdsRequestOutcome.AlreadyRunning, false, false)]
        public void AdsShowResult_ExposesRewardAndContinuationSemantics(
            AdsRequestOutcome outcome,
            bool expectedReward,
            bool expectedContinue)
        {
            var id = AdsRequestId.Create("scope", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "nonce");
            var result = new AdsShowResult(id, outcome, expectedReward, "code", "evidence");

            Assert.AreEqual(expectedReward, result.IsEarnedReward);
            Assert.AreEqual(expectedContinue, result.ShouldContinueFlow);
        }

        [Test]
        public void AdsShowRequest_DerivesKindAndPlacementFromId()
        {
            var id = AdsRequestId.Create("scope", AdsRequestKind.Interstitial, Placement.IN_GAME, "nonce");
            var request = new AdsShowRequest(id, "level-end");

            Assert.AreEqual(AdsRequestKind.Interstitial, request.Kind);
            Assert.AreEqual(Placement.IN_GAME, request.Placement);
        }

        [Test]
        public void CompletedInterstitial_NeverReportsEarnedReward()
        {
            var id = AdsRequestId.Create("inter", AdsRequestKind.Interstitial, Placement.IN_GAME, "nonce");
            var result = new AdsShowResult(id, AdsRequestOutcome.Completed, true);

            Assert.IsFalse(result.IsEarnedReward);
            Assert.IsTrue(result.ShouldContinueFlow);
        }
    }
}

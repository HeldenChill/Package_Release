using NUnit.Framework;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public sealed class AdsRequestControllerTests
    {
        [Test]
        public void Complete_TerminalOnlyOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "1"));
            int callbacks = 0;
            Assert.IsTrue(controller.TryBegin(request, _ => callbacks++, out var context, out _));
            context.MarkRewardEarned("provider-reward-id");

            Assert.AreEqual(AdsRequestOutcome.Completed, context.Complete(AdsRequestOutcome.Completed, "closed").Outcome);
            Assert.AreEqual(AdsRequestOutcome.DuplicateIgnored, context.Complete(AdsRequestOutcome.Failed, "late-fail").Outcome);
            Assert.AreEqual(1, context.TerminalCount);
            Assert.AreEqual(1, callbacks);
        }

        [Test]
        public void Begin_RejectsConcurrentSameKind()
        {
            var controller = new AdsRequestController();
            var first = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "1"));
            var second = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "2"));

            Assert.IsTrue(controller.TryBegin(first, _ => { }, out var firstContext, out _));
            Assert.IsFalse(controller.TryBegin(second, _ => { }, out var secondContext, out var secondResult));

            Assert.AreEqual(first.RequestId, firstContext.Request.RequestId);
            Assert.AreEqual(AdsRequestOutcome.AlreadyRunning, secondResult.Outcome);
            Assert.IsNull(secondContext);
        }
    }
}

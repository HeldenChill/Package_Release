using Hung.Base;

namespace Hung.Ads
{
    public sealed class RewardedRequestSession
    {
        private readonly AdsRequestContext context;

        public RewardedRequestSession(AdsRequestContext context)
        {
            this.context = context;
        }

        public AdsShowResult OnRewardEarned(string evidence)
        {
            context.MarkRewardEarned(evidence);
            return context.Status;
        }

        public AdsShowResult OnHidden()
        {
            return context.Complete(context.RewardEarned ? AdsRequestOutcome.Completed : AdsRequestOutcome.Skipped, "reward-hidden");
        }

        public AdsShowResult OnDisplayFailed()
        {
            return context.Complete(AdsRequestOutcome.Failed, "display-failed");
        }

        public AdsShowResult OnUnavailable()
        {
            return context.Complete(AdsRequestOutcome.Unavailable, "provider-unavailable");
        }
    }
}

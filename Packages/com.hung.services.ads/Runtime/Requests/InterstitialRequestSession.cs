using Hung.Base;

namespace Hung.Ads
{
    public sealed class InterstitialRequestSession
    {
        private readonly AdsRequestContext context;

        public InterstitialRequestSession(AdsRequestContext context)
        {
            this.context = context;
        }

        public AdsShowResult OnDone() => context.Complete(AdsRequestOutcome.Completed, "inter-closed");
        public AdsShowResult OnDisplayFailed() => context.Complete(AdsRequestOutcome.Failed, "display-failed");
        public AdsShowResult OnUnavailable() => context.Complete(AdsRequestOutcome.Unavailable, "provider-unavailable");
        public AdsShowResult OnSkipped(string diagnosticCode) => context.Complete(AdsRequestOutcome.Skipped, diagnosticCode);
    }
}

using System;
using Hung.Base;

namespace Hung.Ads
{
    public sealed class AdsRequestContext
    {
        private readonly Action<AdsShowResult> onCompleted;
        private readonly Action<AdsRequestContext> onTerminal;
        private bool rewardEarned;
        private string providerEvidence = string.Empty;

        internal AdsRequestContext(AdsShowRequest request, Action<AdsShowResult> onCompleted, Action<AdsRequestContext> onTerminal)
        {
            Request = request;
            this.onCompleted = onCompleted;
            this.onTerminal = onTerminal;
        }

        public AdsShowRequest Request { get; }
        public AdsShowResult Status { get; private set; }
        public int TerminalCount { get; private set; }
        public bool IsTerminal => TerminalCount != 0;
        public bool RewardEarned => rewardEarned;

        public void MarkRewardEarned(string evidence)
        {
            if (IsTerminal || Request.Kind != AdsRequestKind.Rewarded) return;
            rewardEarned = true;
            providerEvidence = evidence ?? string.Empty;
        }

        public AdsShowResult Complete(AdsRequestOutcome outcome, string diagnosticCode = null, string evidence = null)
        {
            if (IsTerminal)
            {
                return new AdsShowResult(Request.RequestId, AdsRequestOutcome.DuplicateIgnored, false, diagnosticCode, evidence);
            }

            TerminalCount++;
            Status = new AdsShowResult(Request.RequestId, outcome, rewardEarned, diagnosticCode, evidence ?? providerEvidence);
            onTerminal?.Invoke(this);
            onCompleted?.Invoke(Status);
            return Status;
        }
    }
}

using System;

namespace Hung.Base
{
    /// <summary>
    /// Minimal proof that an external reward authorization completed before a claim starts.
    /// </summary>
    public readonly struct RewardAuthorization
    {
        public RewardAuthorization(string requestId, Placement placement, DateTime completedUtc, string evidence)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Authorization request ID cannot be empty.", nameof(requestId));
            if (completedUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Authorization completion time must be UTC.", nameof(completedUtc));

            RequestId = requestId;
            Placement = placement;
            CompletedUtc = completedUtc;
            ProviderEvidence = evidence;
        }

        public string RequestId { get; }

        public Placement Placement { get; }

        public DateTime CompletedUtc { get; }

        public string ProviderEvidence { get; }
    }
}

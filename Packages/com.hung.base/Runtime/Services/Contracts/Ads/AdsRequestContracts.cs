using System;

namespace Hung.Base
{
    public enum AdsRequestKind { Rewarded = 1, Interstitial = 2, Banner = 3, AppOpen = 4 }

    public enum AdsRequestOutcome
    {
        Completed = 1,
        Skipped = 2,
        Failed = 3,
        Unavailable = 4,
        Unsupported = 5,
        Misconfigured = 6,
        DuplicateIgnored = 7,
        AlreadyRunning = 8
    }

    public readonly struct AdsRequestId : IEquatable<AdsRequestId>
    {
        public AdsRequestId(string value, AdsRequestKind kind, Placement placement)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Ads request id is required.", nameof(value));
            Value = value;
            Kind = kind;
            Placement = placement;
        }

        public string Value { get; }
        public AdsRequestKind Kind { get; }
        public Placement Placement { get; }

        public static AdsRequestId Create(string scope, AdsRequestKind kind, Placement placement, string nonce)
        {
            if (string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Scope is required.", nameof(scope));
            if (string.IsNullOrWhiteSpace(nonce)) throw new ArgumentException("Nonce is required.", nameof(nonce));
            string canonical = $"{scope.Trim()}|{(int)kind}|{(int)placement}|{nonce.Trim()}";
            return new AdsRequestId(StableHash.Hex64(canonical), kind, placement);
        }

        public bool Equals(AdsRequestId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AdsRequestId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public readonly struct AdsShowRequest
    {
        public AdsShowRequest(AdsRequestId requestId, string context = null)
        {
            if (string.IsNullOrWhiteSpace(requestId.Value)) throw new ArgumentException("Ads request id is required.", nameof(requestId));
            RequestId = requestId;
            Context = context ?? string.Empty;
        }

        public AdsRequestId RequestId { get; }
        public AdsRequestKind Kind => RequestId.Kind;
        public Placement Placement => RequestId.Placement;
        public string Context { get; }
    }

    public readonly struct AdsShowResult
    {
        public AdsShowResult(AdsRequestId requestId, AdsRequestOutcome outcome, bool rewardEarned = false, string diagnosticCode = null, string providerEvidence = null)
        {
            RequestId = requestId;
            Outcome = outcome;
            RewardEarned = rewardEarned;
            DiagnosticCode = diagnosticCode ?? string.Empty;
            ProviderEvidence = providerEvidence ?? string.Empty;
        }

        public AdsRequestId RequestId { get; }
        public AdsRequestKind Kind => RequestId.Kind;
        public Placement Placement => RequestId.Placement;
        public AdsRequestOutcome Outcome { get; }
        public bool RewardEarned { get; }
        public string DiagnosticCode { get; }
        public string ProviderEvidence { get; }
        public bool IsEarnedReward => Kind == AdsRequestKind.Rewarded && Outcome == AdsRequestOutcome.Completed && RewardEarned;
        public bool ShouldContinueFlow => Outcome == AdsRequestOutcome.Completed || Outcome == AdsRequestOutcome.Skipped || Outcome == AdsRequestOutcome.Unavailable || Outcome == AdsRequestOutcome.Unsupported;
    }

    public interface IAdsRequestService
    {
        void ShowRewarded(AdsShowRequest request, Action<AdsShowResult> onCompleted);
        void ShowInterstitial(AdsShowRequest request, Action<AdsShowResult> onCompleted);
    }

    internal static class StableHash
    {
        public static string Hex64(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= prime;
            }
            return hash.ToString("x16");
        }
    }
}

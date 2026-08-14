using System;
using System.Collections.Generic;

namespace Hung.Base
{
    /// <summary>
    /// Request to claim a reward through the recoverable coordinator.
    /// </summary>
    public readonly struct RewardClaimRequest
    {
        public RewardClaimRequest(RewardClaimId id, string feature, IReadOnlyList<RewardGrantItem> items, string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(feature))
                throw new ArgumentException("Reward feature cannot be empty.", nameof(feature));
            if (items == null || items.Count == 0)
                throw new ArgumentException("Reward claim must contain at least one item.", nameof(items));
            if (string.IsNullOrWhiteSpace(fingerprint))
                throw new ArgumentException("Reward payload fingerprint cannot be empty.", nameof(fingerprint));

            ClaimId = id;
            Feature = feature;
            Items = items;
            PayloadFingerprint = fingerprint;
        }

        public RewardClaimId ClaimId { get; }

        public string Feature { get; }

        public IReadOnlyList<RewardGrantItem> Items { get; }

        public string PayloadFingerprint { get; }
    }

    /// <summary>
    /// Result of persisting package-specific feature state after value is durably granted.
    /// </summary>
    public readonly struct RewardFeatureCommitResult
    {
        public RewardFeatureCommitResult(bool success, string diagnosticCode = null)
        {
            Success = success;
            DiagnosticCode = diagnosticCode;
        }

        public bool Success { get; }

        public string DiagnosticCode { get; }
    }

    /// <summary>
    /// High-level reward claim outcome.
    /// </summary>
    public readonly struct RewardClaimResult
    {
        public RewardClaimResult(RewardGrantOutcome outcome, string diagnosticCode = null)
        {
            Outcome = outcome;
            DiagnosticCode = diagnosticCode;
        }

        public RewardGrantOutcome Outcome { get; }

        public string DiagnosticCode { get; }

        public bool Success => Outcome == RewardGrantOutcome.Success || Outcome == RewardGrantOutcome.IdempotentReplay;
    }

    /// <summary>
    /// Summary of pending reward recovery work.
    /// </summary>
    public readonly struct RewardRecoveryReport
    {
        public RewardRecoveryReport(int recoveredCount, int failedCount)
        {
            RecoveredCount = recoveredCount;
            FailedCount = failedCount;
        }

        public int RecoveredCount { get; }

        public int FailedCount { get; }
    }

    /// <summary>
    /// Coordinates recoverable reward claims. It is not a physical cross-store transaction.
    /// </summary>
    public interface IRewardClaimCoordinator
    {
        RewardClaimResult Claim(RewardClaimRequest request);

        RewardClaimResult Finalize(RewardClaimId id, Func<RewardFeatureCommitResult> persistFeatureState);

        RewardRecoveryReport RecoverPending();
    }
}

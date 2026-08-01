using System;
using System.Collections.Generic;

namespace Hung.Base
{
    /// <summary>
    /// Outcome reported by an idempotent reward grant participant.
    /// </summary>
    public enum RewardGrantOutcome
    {
        Success,
        IdempotentReplay,
        Conflict,
        InvalidReward,
        PersistenceFailure,
        Unavailable
    }

    /// <summary>
    /// Immutable item payload for a reward grant.
    /// </summary>
    public readonly struct RewardGrantItem
    {
        public RewardGrantItem(ItemId itemId, int quantity)
        {
            if (!itemId.IsValid)
                throw new ArgumentException("Reward item ID must be valid.", nameof(itemId));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            ItemId = itemId;
            Quantity = quantity;
        }

        public ItemId ItemId { get; }

        public int Quantity { get; }
    }

    /// <summary>
    /// Result returned by a reward grant participant.
    /// </summary>
    public readonly struct RewardGrantResult
    {
        public RewardGrantResult(RewardGrantOutcome outcome, string code = null)
        {
            Outcome = outcome;
            DiagnosticCode = code;
        }

        public RewardGrantOutcome Outcome { get; }

        public string DiagnosticCode { get; }
    }

    /// <summary>
    /// Game-side participant that durably grants item value by stable reward claim ID.
    /// </summary>
    public interface IRewardGrantService
    {
        RewardGrantResult Grant(RewardClaimId id, IReadOnlyList<RewardGrantItem> items, string fingerprint);
    }
}

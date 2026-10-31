using System;
using Hung.Base;
namespace Hung.Data.Rewards
{
    // Feature callers serialize access. This helper owns no eligibility or product state.
    public static class RewardOperation
    {
        public static RewardClaimResult Complete<T>(RewardStateStore<T> store, IRewardClaimCoordinator coordinator,
            string profile, string feature, Func<T, PendingRewardData> pending,
            Action<T, PendingRewardData> apply, Func<T, string> consumed) where T : new()
        {
            if (!store.Load(out var state, out var code)) return Failure(code);
            var p = pending(state);
            if (p == null) return new RewardClaimResult(RewardGrantOutcome.Unavailable, "REWARD_NOT_PREPARED");
            if (!p.Valid() || p.profile != profile || p.feature != feature)
                return new RewardClaimResult(RewardGrantOutcome.Unavailable, "REWARD_CONTEXT_MISMATCH");
            var claim = coordinator.Claim(p.Request());
            if (!claim.Success) return claim;
            var finalized = coordinator.Finalize(new RewardClaimId(p.claimId), () =>
            {
                if (!store.Load(out var current, out var loadCode)) return new RewardFeatureCommitResult(false, loadCode);
                var currentPending = pending(current);
                if (currentPending == null || currentPending.claimId != p.claimId || currentPending.fingerprint != p.fingerprint)
                    return new RewardFeatureCommitResult(false, "REWARD_PENDING_CHANGED");
                if (consumed(current) != p.claimId) apply(current, p);
                // Retain pending until Finalized is durable. Retrying this callback is idempotent.
                bool saved = store.Save(current, out var saveCode);
                return new RewardFeatureCommitResult(saved, saveCode);
            });
            if (!finalized.Success) return finalized;
            return claim.Outcome == RewardGrantOutcome.IdempotentReplay
                ? new RewardClaimResult(RewardGrantOutcome.IdempotentReplay) : finalized;
        }
        public static RewardClaimResult Failure(string diagnostic) => new RewardClaimResult(RewardGrantOutcome.PersistenceFailure, diagnostic);
        public static RewardClaimResult Unavailable(string diagnostic) => new RewardClaimResult(RewardGrantOutcome.Unavailable, diagnostic);
        public static bool Authorized(RewardAuthorization? authorization, Placement placement, DateTime now)
            => authorization.HasValue && !string.IsNullOrWhiteSpace(authorization.Value.RequestId)
            && authorization.Value.Placement == placement && !string.IsNullOrWhiteSpace(authorization.Value.ProviderEvidence)
            && authorization.Value.CompletedUtc.Kind == DateTimeKind.Utc && authorization.Value.CompletedUtc <= now.AddMinutes(5);
    }
}

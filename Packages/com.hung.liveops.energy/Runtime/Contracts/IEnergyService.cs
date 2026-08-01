using System;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Public Energy service boundary. The only way PVM (or any consumer) interacts with
    /// Energy state — all mutations are copy-on-write and persist-before-publish. Obtained
    /// only through <see cref="EnergyServiceFactory"/>; no implementation of this interface
    /// may grant Unlimited/permissive Energy by default (see <c>ai-guardrails.md</c> and the
    /// plan's Global Constraints).
    /// </summary>
    public interface IEnergyService
    {
        /// <summary>Current authoritative Energy state.</summary>
        EnergySnapshot Current { get; }

        /// <summary>Reserves a run, spending renewable-first then bonus for its configured cost. Idempotent on <paramref name="runId"/>.</summary>
        EnergyRunStartResult TryStartRun(string runId);

        /// <summary>Marks gameplay entry for the given run. Must be called after <see cref="TryStartRun"/> succeeded.</summary>
        EnergyRunEntryResult MarkRunEntered(string runId);

        /// <summary>Finalizes the given run with the given outcome, clearing the active-run reservation.</summary>
        EnergyRunCompletionResult CompleteRun(string runId, RunOutcome outcome);

        /// <summary>Cancels a run that failed to start (before gameplay entry) and refunds its reservation.</summary>
        EnergyRunCancellationResult CancelFailedStart(string runId);

        /// <summary>Grants renewable Energy, capped at the configured maximum. Idempotent on <paramref name="transactionId"/>.</summary>
        EnergyGrantResult AddRenewable(int amount, string transactionId);

        /// <summary>Grants bonus Energy, uncapped. Idempotent on <paramref name="transactionId"/>.</summary>
        EnergyGrantResult AddBonus(int amount, string transactionId);

        /// <summary>Grants (or extends) Unlimited Energy for the given duration. Idempotent on <paramref name="transactionId"/>.</summary>
        EnergyGrantResult GrantUnlimited(TimeSpan duration, string transactionId);

        /// <summary>Applies elapsed-time regeneration/expiry against the current clock and returns the resulting snapshot.</summary>
        EnergySnapshot Reconcile();

        /// <summary>Raised after every successfully persisted state change, with the new snapshot.</summary>
        event Action<EnergySnapshot> Changed;
    }
}

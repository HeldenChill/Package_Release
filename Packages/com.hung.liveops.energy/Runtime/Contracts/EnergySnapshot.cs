using System;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Immutable snapshot of the currently active run, as exposed on <see cref="EnergySnapshot"/>.
    /// All fields are default/zero when <see cref="HasActiveRun"/> is false.
    /// </summary>
    public readonly struct EnergyActiveRunSnapshot
    {
        /// <summary>True if a run is currently reserved (started but not yet completed/cancelled).</summary>
        public bool HasActiveRun { get; }

        /// <summary>The active run's caller-provided ID. Empty when <see cref="HasActiveRun"/> is false.</summary>
        public string RunId { get; }

        /// <summary>True if the active run was reserved at zero cost (a "free run").</summary>
        public bool IsFree { get; }

        /// <summary>True if gameplay entry has been marked for the active run.</summary>
        public bool EnteredGameplay { get; }

        public EnergyActiveRunSnapshot(bool hasActiveRun, string runId, bool isFree, bool enteredGameplay)
        {
            HasActiveRun = hasActiveRun;
            RunId = runId ?? string.Empty;
            IsFree = isFree;
            EnteredGameplay = enteredGameplay;
        }

        /// <summary>The canonical "no active run" value.</summary>
        public static EnergyActiveRunSnapshot None => new EnergyActiveRunSnapshot(false, string.Empty, false, false);
    }

    /// <summary>
    /// Immutable, public read-only view of current Energy state. Returned by every
    /// <see cref="IEnergyService"/> command and by <see cref="IEnergyService.Current"/>/
    /// <see cref="IEnergyService.Changed"/>. Never carries a mutable reference into the
    /// service's internal aggregate.
    /// </summary>
    public readonly struct EnergySnapshot
    {
        /// <summary>Renewable (regenerating, capped) Energy currently owned.</summary>
        public int RenewableAmount { get; }

        /// <summary>Bonus (non-regenerating, uncapped) Energy currently owned.</summary>
        public int BonusAmount { get; }

        /// <summary>RenewableAmount + BonusAmount.</summary>
        public int Total => RenewableAmount + BonusAmount;

        /// <summary>Configured maximum for <see cref="RenewableAmount"/>.</summary>
        public int RenewableMax { get; }

        /// <summary>True if an active Unlimited Energy grant has not yet expired.</summary>
        public bool IsUnlimitedActive { get; }

        /// <summary>UTC expiry of the current Unlimited grant, or null if none is active.</summary>
        public DateTime? UnlimitedExpiryUtc { get; }

        /// <summary>UTC time of the next renewable regeneration tick, or null if already at <see cref="RenewableMax"/>.</summary>
        public DateTime? NextRegenerationUtc { get; }

        /// <summary>True if the last reconciliation observed the clock moving backwards relative to prior state.</summary>
        public bool RollbackDetected { get; }

        /// <summary>Summary of the currently reserved run, if any.</summary>
        public EnergyActiveRunSnapshot ActiveRun { get; }

        public EnergySnapshot(
            int renewableAmount,
            int bonusAmount,
            int renewableMax,
            bool isUnlimitedActive,
            DateTime? unlimitedExpiryUtc,
            DateTime? nextRegenerationUtc,
            bool rollbackDetected,
            EnergyActiveRunSnapshot activeRun)
        {
            RenewableAmount = renewableAmount;
            BonusAmount = bonusAmount;
            RenewableMax = renewableMax;
            IsUnlimitedActive = isUnlimitedActive;
            UnlimitedExpiryUtc = unlimitedExpiryUtc;
            NextRegenerationUtc = nextRegenerationUtc;
            RollbackDetected = rollbackDetected;
            ActiveRun = activeRun;
        }
    }
}

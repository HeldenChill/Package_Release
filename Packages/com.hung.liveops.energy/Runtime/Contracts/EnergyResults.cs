namespace Hung.LiveOps.Energy
{
    /// <summary>Result of <see cref="IEnergyService.TryStartRun"/>.</summary>
    public readonly struct EnergyRunStartResult
    {
        public EnergyResultOutcome Outcome { get; }

        /// <summary>True if the reserved run was free (zero cost). Only meaningful when <see cref="Outcome"/> is Success or IdempotentReplay.</summary>
        public bool IsFreeRun { get; }

        public EnergySnapshot Snapshot { get; }

        public EnergyRunStartResult(EnergyResultOutcome outcome, bool isFreeRun, EnergySnapshot snapshot)
        {
            Outcome = outcome;
            IsFreeRun = isFreeRun;
            Snapshot = snapshot;
        }
    }

    /// <summary>Result of <see cref="IEnergyService.MarkRunEntered"/>.</summary>
    public readonly struct EnergyRunEntryResult
    {
        public EnergyResultOutcome Outcome { get; }

        public EnergyRunEntryResult(EnergyResultOutcome outcome)
        {
            Outcome = outcome;
        }
    }

    /// <summary>Result of <see cref="IEnergyService.CompleteRun"/>.</summary>
    public readonly struct EnergyRunCompletionResult
    {
        public EnergyResultOutcome Outcome { get; }

        /// <summary>The outcome recorded in the ledger for this run. Null if no run was found to complete.</summary>
        public RunOutcome? RecordedOutcome { get; }

        public EnergySnapshot Snapshot { get; }

        public EnergyRunCompletionResult(EnergyResultOutcome outcome, RunOutcome? recordedOutcome, EnergySnapshot snapshot)
        {
            Outcome = outcome;
            RecordedOutcome = recordedOutcome;
            Snapshot = snapshot;
        }
    }

    /// <summary>Result of <see cref="IEnergyService.CancelFailedStart"/>.</summary>
    public readonly struct EnergyRunCancellationResult
    {
        public EnergyResultOutcome Outcome { get; }

        public EnergySnapshot Snapshot { get; }

        public EnergyRunCancellationResult(EnergyResultOutcome outcome, EnergySnapshot snapshot)
        {
            Outcome = outcome;
            Snapshot = snapshot;
        }
    }

    /// <summary>Result of <see cref="IEnergyService.AddRenewable"/>, <see cref="IEnergyService.AddBonus"/>, and <see cref="IEnergyService.GrantUnlimited"/>.</summary>
    public readonly struct EnergyGrantResult
    {
        public EnergyResultOutcome Outcome { get; }

        public EnergySnapshot Snapshot { get; }

        public EnergyGrantResult(EnergyResultOutcome outcome, EnergySnapshot snapshot)
        {
            Outcome = outcome;
            Snapshot = snapshot;
        }
    }
}

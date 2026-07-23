namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Outcome of an <see cref="IEnergyService"/> command. Shared across all result types
    /// so callers can branch on a single enum regardless of which command produced it.
    /// </summary>
    public enum EnergyResultOutcome
    {
        /// <summary>Command applied and persisted.</summary>
        Success,

        /// <summary>Command ID/kind/payload matched a prior recorded transaction; no new mutation applied.</summary>
        IdempotentReplay,

        /// <summary>Not enough Energy to satisfy the command.</summary>
        Insufficient,

        /// <summary>Arguments failed validation (e.g. non-positive amount, empty run ID).</summary>
        InvalidInput,

        /// <summary>Command ID was reused with a different command kind or payload.</summary>
        Conflict,

        /// <summary>Service configuration is missing or invalid; command cannot be evaluated.</summary>
        ConfigurationFailure,

        /// <summary>Observed clock time moved backwards relative to previously recorded state.</summary>
        ClockRollback,

        /// <summary>The mutation was valid but persisting the candidate state failed; current state is unchanged.</summary>
        PersistenceFailure,

        /// <summary>The service itself could not be constructed/is not usable.</summary>
        Unavailable
    }
}

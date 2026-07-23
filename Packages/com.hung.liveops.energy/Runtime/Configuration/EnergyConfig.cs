using System;
using System.Globalization;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Immutable, validated Energy configuration. Cannot be constructed with invalid values —
    /// the constructor throws <see cref="ArgumentException"/> with an actionable message.
    /// This guarantees no code path can produce a permissive/unlimited config by accident.
    /// </summary>
    public sealed class EnergyConfig
    {
        /// <summary>
        /// Maximum amount of renewable Energy. Regeneration pauses while the renewable
        /// balance is at or above this value. Bonus Energy is not included in this cap.
        /// </summary>
        public int RenewableMax { get; }

        /// <summary>
        /// Elapsed time required to regenerate one renewable Energy.
        /// </summary>
        public TimeSpan RegenerationInterval { get; }

        /// <summary>
        /// Energy reserved when starting a run. Renewable Energy is spent before bonus Energy.
        /// A value of zero makes runs free.
        /// </summary>
        public int RunCost { get; }

        /// <summary>
        /// Renewable Energy granted when no persisted Energy state exists. This value may exceed
        /// <see cref="RenewableMax"/>; regeneration remains paused until the balance falls below the cap.
        /// </summary>
        public int InitialRenewable { get; }

        /// <summary>
        /// Uncapped, non-regenerating bonus Energy granted when no persisted Energy state exists.
        /// </summary>
        public int InitialBonus { get; }

        /// <summary>
        /// Maximum number of completed transaction records retained for idempotency and conflict
        /// detection. Older finalized records are removed when this positive limit is exceeded.
        /// </summary>
        public int TransactionRetentionCapacity { get; }

        public EnergyConfig(
            int renewableMax,
            TimeSpan regenerationInterval,
            int runCost,
            int initialRenewable,
            int initialBonus,
            int transactionRetentionCapacity)
        {
            if (renewableMax <= 0)
            {
                throw new ArgumentException(
                    $"EnergyConfig: renewableMax must be > 0, got {renewableMax}.", nameof(renewableMax));
            }
            if (regenerationInterval <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    $"EnergyConfig: regenerationInterval must be > 0, got {regenerationInterval}.", nameof(regenerationInterval));
            }
            if (runCost < 0)
            {
                throw new ArgumentException(
                    $"EnergyConfig: runCost must be >= 0, got {runCost}.", nameof(runCost));
            }
            if (initialRenewable < 0)
            {
                throw new ArgumentException(
                    $"EnergyConfig: initialRenewable must be >= 0, got {initialRenewable}.", nameof(initialRenewable));
            }
            if (initialBonus < 0)
            {
                throw new ArgumentException(
                    $"EnergyConfig: initialBonus must be >= 0, got {initialBonus}.", nameof(initialBonus));
            }
            if (transactionRetentionCapacity <= 0)
            {
                throw new ArgumentException(
                    $"EnergyConfig: transactionRetentionCapacity must be > 0, got {transactionRetentionCapacity}.", nameof(transactionRetentionCapacity));
            }

            RenewableMax = renewableMax;
            RegenerationInterval = regenerationInterval;
            RunCost = runCost;
            InitialRenewable = initialRenewable;
            InitialBonus = initialBonus;
            TransactionRetentionCapacity = transactionRetentionCapacity;
        }

        /// <summary>
        /// Deterministic version string derived from the actual configured values.
        /// Two configs with identical values always produce identical strings;
        /// any differing value changes the string. Used by reconciliation to detect
        /// "did the config change since it was last applied" via string inequality.
        /// </summary>
        public string ComputeVersion()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}",
                RenewableMax,
                RegenerationInterval.Ticks,
                RunCost,
                InitialRenewable,
                InitialBonus,
                TransactionRetentionCapacity);
        }
    }
}

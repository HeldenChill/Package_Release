using System;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Primitive-only, JsonUtility-safe wire representation of <see cref="EnergyState"/>.
    /// Every field is int/long/bool/string or an array of primitive-only structs.
    /// No DateTime, no Nullable, no Dictionary, no polymorphic fields.
    /// UTC timestamps are stored as ticks; 0 means "not set".
    /// </summary>
    [Serializable]
    internal sealed class EnergyStateDto
    {
        public int schemaVersion;
        public int renewableAmount;
        public int bonusAmount;
        public long regenerationAnchorUtcTicks;
        public long unlimitedUntilUtcTicks;
        public long latestObservedUtcTicks;
        public string appliedConfigSnapshot;

        public bool hasActiveRun;
        public string activeRunId;
        public bool activeRunIsFree;
        public int activeRunReservedRenewable;
        public int activeRunReservedBonus;
        public bool activeRunEnteredGameplay;

        public TransactionRecordDto[] transactionLedger;
    }

    /// <summary>
    /// Primitive-only, JsonUtility-safe wire representation of a
    /// <see cref="TransactionRecord"/> ledger entry.
    /// </summary>
    [Serializable]
    internal struct TransactionRecordDto
    {
        public string id;
        public string commandKind;
        public string payloadFingerprint;
        public string outcome;
        public string runOutcome;
        public int reservedRenewable;
        public int reservedBonus;
        public bool isFreeRun;
    }
}

using System;
using System.Collections.Generic;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Runtime aggregate for Energy state. Never serialized directly — use
    /// <see cref="EnergyStateMapper"/> to convert to/from <see cref="EnergyStateDto"/>
    /// for persistence.
    /// </summary>
    internal sealed class EnergyState
    {
        public int SchemaVersion;
        public int RenewableAmount;
        public int BonusAmount;
        public DateTime? RegenerationAnchorUtc;
        public DateTime? UnlimitedUntilUtc;
        public DateTime? LatestObservedUtc;
        public string AppliedConfigSnapshot = string.Empty;
        public ActiveRun ActiveRun;
        public List<TransactionRecord> TransactionLedger = new List<TransactionRecord>();

        /// <summary>
        /// Returns a fully detached copy via a DTO round-trip (<see cref="EnergyStateMapper.ToDto"/>
        /// then <see cref="EnergyStateMapper.FromDto"/>), not a hand-written field copy — this keeps
        /// clone correctness covered by the mapper's own round-trip tests and unable to drift from
        /// the persistence path.
        /// </summary>
        public EnergyState DeepClone()
        {
            EnergyStateDto dto = EnergyStateMapper.ToDto(this);
            return EnergyStateMapper.FromDto(dto);
        }
    }

    /// <summary>
    /// Represents a reserved, in-progress run. Null (or absent) means no active run.
    /// </summary>
    internal sealed class ActiveRun
    {
        public string RunId;
        public bool IsFree;
        public int ReservedRenewable;
        public int ReservedBonus;
        public bool EnteredGameplay;
    }

    /// <summary>
    /// One ledger entry recording the outcome of a processed command, used for
    /// idempotent replay detection.
    /// </summary>
    internal sealed class TransactionRecord
    {
        public string Id;
        public string CommandKind;
        public string PayloadFingerprint;
        public string Outcome;
        public string RunOutcome;
        public int ReservedRenewable;
        public int ReservedBonus;
        public bool IsFreeRun;
    }
}

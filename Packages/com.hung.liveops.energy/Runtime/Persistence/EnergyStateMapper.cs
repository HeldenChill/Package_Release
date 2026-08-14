using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Result of attempting to parse a persisted Energy state JSON payload.
    /// </summary>
    internal enum EnergyStateDtoParseOutcome
    {
        Ok,
        Corrupt,
        UnsupportedVersion
    }

    /// <summary>
    /// The only place that converts between the runtime <see cref="EnergyState"/>
    /// aggregate and its primitive-only <see cref="EnergyStateDto"/> wire form.
    /// </summary>
    internal static class EnergyStateMapper
    {
        // ponytail: single supported schema version for now; bump + branch here when a real migration is needed.
        public const int CurrentSchemaVersion = 1;

        public static EnergyStateDto ToDto(EnergyState state)
        {
            EnergyStateDto dto = new EnergyStateDto
            {
                schemaVersion = state.SchemaVersion,
                renewableAmount = state.RenewableAmount,
                bonusAmount = state.BonusAmount,
                regenerationAnchorUtcTicks = state.RegenerationAnchorUtc?.Ticks ?? 0,
                unlimitedUntilUtcTicks = state.UnlimitedUntilUtc?.Ticks ?? 0,
                latestObservedUtcTicks = state.LatestObservedUtc?.Ticks ?? 0,
                appliedConfigSnapshot = state.AppliedConfigSnapshot ?? string.Empty,
                hasActiveRun = state.ActiveRun != null
            };

            if (state.ActiveRun != null)
            {
                dto.activeRunId = state.ActiveRun.RunId;
                dto.activeRunIsFree = state.ActiveRun.IsFree;
                dto.activeRunReservedRenewable = state.ActiveRun.ReservedRenewable;
                dto.activeRunReservedBonus = state.ActiveRun.ReservedBonus;
                dto.activeRunEnteredGameplay = state.ActiveRun.EnteredGameplay;
            }

            int ledgerCount = state.TransactionLedger?.Count ?? 0;
            dto.transactionLedger = new TransactionRecordDto[ledgerCount];
            for (int i = 0; i < ledgerCount; i++)
            {
                TransactionRecord record = state.TransactionLedger[i];
                dto.transactionLedger[i] = new TransactionRecordDto
                {
                    id = record.Id,
                    commandKind = record.CommandKind,
                    payloadFingerprint = record.PayloadFingerprint,
                    outcome = record.Outcome,
                    runOutcome = record.RunOutcome,
                    reservedRenewable = record.ReservedRenewable,
                    reservedBonus = record.ReservedBonus,
                    isFreeRun = record.IsFreeRun
                };
            }

            return dto;
        }

        public static EnergyState FromDto(EnergyStateDto dto)
        {
            EnergyState state = new EnergyState
            {
                SchemaVersion = dto.schemaVersion,
                RenewableAmount = dto.renewableAmount,
                BonusAmount = dto.bonusAmount,
                RegenerationAnchorUtc = dto.regenerationAnchorUtcTicks == 0
                    ? (DateTime?)null
                    : new DateTime(dto.regenerationAnchorUtcTicks, DateTimeKind.Utc),
                UnlimitedUntilUtc = dto.unlimitedUntilUtcTicks == 0
                    ? (DateTime?)null
                    : new DateTime(dto.unlimitedUntilUtcTicks, DateTimeKind.Utc),
                LatestObservedUtc = dto.latestObservedUtcTicks == 0
                    ? (DateTime?)null
                    : new DateTime(dto.latestObservedUtcTicks, DateTimeKind.Utc),
                AppliedConfigSnapshot = dto.appliedConfigSnapshot ?? string.Empty
            };

            if (dto.hasActiveRun)
            {
                state.ActiveRun = new ActiveRun
                {
                    RunId = dto.activeRunId,
                    IsFree = dto.activeRunIsFree,
                    ReservedRenewable = dto.activeRunReservedRenewable,
                    ReservedBonus = dto.activeRunReservedBonus,
                    EnteredGameplay = dto.activeRunEnteredGameplay
                };
            }

            int ledgerCount = dto.transactionLedger?.Length ?? 0;
            state.TransactionLedger = new List<TransactionRecord>(ledgerCount);
            for (int i = 0; i < ledgerCount; i++)
            {
                TransactionRecordDto recordDto = dto.transactionLedger[i];
                state.TransactionLedger.Add(new TransactionRecord
                {
                    Id = recordDto.id,
                    CommandKind = recordDto.commandKind,
                    PayloadFingerprint = recordDto.payloadFingerprint,
                    Outcome = recordDto.outcome,
                    RunOutcome = recordDto.runOutcome,
                    ReservedRenewable = recordDto.reservedRenewable,
                    ReservedBonus = recordDto.reservedBonus,
                    IsFreeRun = recordDto.isFreeRun
                });
            }

            return state;
        }

        public static string ToJson(EnergyState state)
        {
            return JsonUtility.ToJson(ToDto(state));
        }

        /// <summary>
        /// Attempts to parse a persisted JSON payload into a runtime <see cref="EnergyState"/>.
        /// Never returns a default/zero state on failure — callers must check <paramref name="outcome"/>.
        /// </summary>
        public static bool TryParseJson(string json, out EnergyState state, out EnergyStateDtoParseOutcome outcome)
        {
            state = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                outcome = EnergyStateDtoParseOutcome.Corrupt;
                return false;
            }

            EnergyStateDto dto;
            try
            {
                dto = JsonUtility.FromJson<EnergyStateDto>(json);
            }
            catch (Exception)
            {
                outcome = EnergyStateDtoParseOutcome.Corrupt;
                return false;
            }

            if (dto == null)
            {
                outcome = EnergyStateDtoParseOutcome.Corrupt;
                return false;
            }

            if (dto.schemaVersion != CurrentSchemaVersion)
            {
                outcome = EnergyStateDtoParseOutcome.UnsupportedVersion;
                return false;
            }

            state = FromDto(dto);
            outcome = EnergyStateDtoParseOutcome.Ok;
            return true;
        }
    }
}

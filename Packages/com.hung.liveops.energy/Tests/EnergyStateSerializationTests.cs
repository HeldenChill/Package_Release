using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Hung.LiveOps.Energy.Tests
{
    internal sealed class EnergyStateSerializationTests
    {
        private static EnergyState BuildFullState()
        {
            EnergyState state = new EnergyState
            {
                SchemaVersion = EnergyStateMapper.CurrentSchemaVersion,
                RenewableAmount = 42,
                BonusAmount = 7,
                RegenerationAnchorUtc = new DateTime(2026, 7, 21, 10, 0, 0, DateTimeKind.Utc),
                UnlimitedUntilUtc = new DateTime(2026, 7, 22, 12, 30, 0, DateTimeKind.Utc),
                LatestObservedUtc = new DateTime(2026, 7, 21, 11, 15, 0, DateTimeKind.Utc),
                AppliedConfigSnapshot = "config-v3",
                ActiveRun = new ActiveRun
                {
                    RunId = "run-123",
                    IsFree = true,
                    ReservedRenewable = 5,
                    ReservedBonus = 1,
                    EnteredGameplay = true
                },
                TransactionLedger = new List<TransactionRecord>
                {
                    new TransactionRecord
                    {
                        Id = "tx-1",
                        CommandKind = "AddRenewable",
                        PayloadFingerprint = "fp-1",
                        Outcome = "Success",
                        RunOutcome = string.Empty,
                        ReservedRenewable = 0,
                        ReservedBonus = 0,
                        IsFreeRun = false
                    },
                    new TransactionRecord
                    {
                        Id = "tx-2",
                        CommandKind = "CompleteRun",
                        PayloadFingerprint = "fp-2",
                        Outcome = "IdempotentReplay",
                        RunOutcome = "Win",
                        ReservedRenewable = 5,
                        ReservedBonus = 1,
                        IsFreeRun = true
                    }
                }
            };
            return state;
        }

        [Test]
        public void ToDto_FromDto_RoundTripsAllFieldsWithActiveRun()
        {
            EnergyState original = BuildFullState();

            EnergyStateDto dto = EnergyStateMapper.ToDto(original);
            EnergyState roundTripped = EnergyStateMapper.FromDto(dto);

            Assert.AreEqual(original.SchemaVersion, roundTripped.SchemaVersion);
            Assert.AreEqual(original.RenewableAmount, roundTripped.RenewableAmount);
            Assert.AreEqual(original.BonusAmount, roundTripped.BonusAmount);
            Assert.AreEqual(original.RegenerationAnchorUtc, roundTripped.RegenerationAnchorUtc);
            Assert.AreEqual(original.UnlimitedUntilUtc, roundTripped.UnlimitedUntilUtc);
            Assert.AreEqual(original.LatestObservedUtc, roundTripped.LatestObservedUtc);
            Assert.AreEqual(original.AppliedConfigSnapshot, roundTripped.AppliedConfigSnapshot);

            Assert.IsNotNull(roundTripped.ActiveRun);
            Assert.AreEqual(original.ActiveRun.RunId, roundTripped.ActiveRun.RunId);
            Assert.AreEqual(original.ActiveRun.IsFree, roundTripped.ActiveRun.IsFree);
            Assert.AreEqual(original.ActiveRun.ReservedRenewable, roundTripped.ActiveRun.ReservedRenewable);
            Assert.AreEqual(original.ActiveRun.ReservedBonus, roundTripped.ActiveRun.ReservedBonus);
            Assert.AreEqual(original.ActiveRun.EnteredGameplay, roundTripped.ActiveRun.EnteredGameplay);

            Assert.AreEqual(original.TransactionLedger.Count, roundTripped.TransactionLedger.Count);
            for (int i = 0; i < original.TransactionLedger.Count; i++)
            {
                TransactionRecord expected = original.TransactionLedger[i];
                TransactionRecord actual = roundTripped.TransactionLedger[i];
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.CommandKind, actual.CommandKind);
                Assert.AreEqual(expected.PayloadFingerprint, actual.PayloadFingerprint);
                Assert.AreEqual(expected.Outcome, actual.Outcome);
                Assert.AreEqual(expected.RunOutcome, actual.RunOutcome);
                Assert.AreEqual(expected.ReservedRenewable, actual.ReservedRenewable);
                Assert.AreEqual(expected.ReservedBonus, actual.ReservedBonus);
                Assert.AreEqual(expected.IsFreeRun, actual.IsFreeRun);
            }
        }

        [Test]
        public void ToDto_FromDto_RoundTripsWithoutActiveRun()
        {
            EnergyState original = BuildFullState();
            original.ActiveRun = null;

            EnergyStateDto dto = EnergyStateMapper.ToDto(original);
            Assert.IsFalse(dto.hasActiveRun);

            EnergyState roundTripped = EnergyStateMapper.FromDto(dto);
            Assert.IsNull(roundTripped.ActiveRun);
        }

        [Test]
        public void ToDto_FromDto_ZeroTicksMeansUnsetTimestamps()
        {
            EnergyState original = BuildFullState();
            original.RegenerationAnchorUtc = null;
            original.UnlimitedUntilUtc = null;
            original.LatestObservedUtc = null;

            EnergyStateDto dto = EnergyStateMapper.ToDto(original);
            Assert.AreEqual(0, dto.regenerationAnchorUtcTicks);
            Assert.AreEqual(0, dto.unlimitedUntilUtcTicks);
            Assert.AreEqual(0, dto.latestObservedUtcTicks);

            EnergyState roundTripped = EnergyStateMapper.FromDto(dto);
            Assert.IsNull(roundTripped.RegenerationAnchorUtc);
            Assert.IsNull(roundTripped.UnlimitedUntilUtc);
            Assert.IsNull(roundTripped.LatestObservedUtc);
        }

        [Test]
        public void TryParseJson_ValidPayload_RoundTripsThroughJsonUtility()
        {
            EnergyState original = BuildFullState();
            string json = EnergyStateMapper.ToJson(original);

            bool success = EnergyStateMapper.TryParseJson(json, out EnergyState parsed, out EnergyStateDtoParseOutcome outcome);

            Assert.IsTrue(success);
            Assert.AreEqual(EnergyStateDtoParseOutcome.Ok, outcome);
            Assert.IsNotNull(parsed);
            Assert.AreEqual(original.RenewableAmount, parsed.RenewableAmount);
            Assert.AreEqual(original.BonusAmount, parsed.BonusAmount);
            Assert.AreEqual(original.ActiveRun.RunId, parsed.ActiveRun.RunId);
            Assert.AreEqual(original.TransactionLedger.Count, parsed.TransactionLedger.Count);
        }

        [Test]
        public void TryParseJson_MalformedJson_ReturnsCorruptAndNullState()
        {
            string malformed = "{ this is not valid json ";

            bool success = EnergyStateMapper.TryParseJson(malformed, out EnergyState parsed, out EnergyStateDtoParseOutcome outcome);

            Assert.IsFalse(success);
            Assert.AreEqual(EnergyStateDtoParseOutcome.Corrupt, outcome);
            Assert.IsNull(parsed);
        }

        [Test]
        public void TryParseJson_EmptyString_ReturnsCorrupt()
        {
            bool success = EnergyStateMapper.TryParseJson(string.Empty, out EnergyState parsed, out EnergyStateDtoParseOutcome outcome);

            Assert.IsFalse(success);
            Assert.AreEqual(EnergyStateDtoParseOutcome.Corrupt, outcome);
            Assert.IsNull(parsed);
        }

        [Test]
        public void TryParseJson_UnsupportedSchemaVersion_ReturnsUnsupportedVersionAndNullState()
        {
            EnergyStateDto dto = new EnergyStateDto
            {
                schemaVersion = EnergyStateMapper.CurrentSchemaVersion + 999,
                transactionLedger = Array.Empty<TransactionRecordDto>()
            };
            string json = JsonUtility.ToJson(dto);

            bool success = EnergyStateMapper.TryParseJson(json, out EnergyState parsed, out EnergyStateDtoParseOutcome outcome);

            Assert.IsFalse(success);
            Assert.AreEqual(EnergyStateDtoParseOutcome.UnsupportedVersion, outcome);
            Assert.IsNull(parsed);
        }
    }
}

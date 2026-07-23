using System;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    [TestFixture]
    internal sealed class EnergyRunTransactionTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static EnergyConfig MakeConfig(
            int renewableMax = 10,
            int regenerationIntervalSeconds = 6000,
            int runCost = 1,
            int initialRenewable = 3,
            int initialBonus = 2,
            int transactionRetentionCapacity = 20)
        {
            return new EnergyConfig(
                renewableMax,
                TimeSpan.FromSeconds(regenerationIntervalSeconds),
                runCost,
                initialRenewable,
                initialBonus,
                transactionRetentionCapacity);
        }

        private static EnergyService MakeService(
            out FakeClock clock,
            out InMemoryEnergyStateStore store,
            EnergyConfig config = null)
        {
            config ??= MakeConfig();
            clock = new FakeClock(BaseUtc);
            store = new InMemoryEnergyStateStore();
            FixedEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());
            return new EnergyService(clock, store, provider);
        }

        [Test]
        public void TryStartRun_Paid_SufficientRenewable_ReservesFromRenewableFirst()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));

            EnergyRunStartResult result = service.TryStartRun("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, result.Outcome);
            Assert.IsFalse(result.IsFreeRun);
            Assert.AreEqual(2, result.Snapshot.RenewableAmount);
            Assert.AreEqual(2, result.Snapshot.BonusAmount);
        }

        [Test]
        public void TryStartRun_Paid_RenewableInsufficientAlone_SplitsAcrossBonus()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 4, initialRenewable: 3, initialBonus: 5));

            EnergyRunStartResult result = service.TryStartRun("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, result.Outcome);
            Assert.AreEqual(0, result.Snapshot.RenewableAmount);
            Assert.AreEqual(4, result.Snapshot.BonusAmount); // 5 - 1 (remainder from cost 4)
        }

        [Test]
        public void TryStartRun_Free_WhenUnlimitedActive_NoBalanceChange()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-unlimited");

            EnergyRunStartResult result = service.TryStartRun("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, result.Outcome);
            Assert.IsTrue(result.IsFreeRun);
            Assert.AreEqual(3, result.Snapshot.RenewableAmount);
            Assert.AreEqual(2, result.Snapshot.BonusAmount);
        }

        [Test]
        public void TryStartRun_Insufficient_NoActiveRun_NoBalanceChange()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 10, initialRenewable: 3, initialBonus: 2));

            EnergyRunStartResult result = service.TryStartRun("run-1");

            Assert.AreEqual(EnergyResultOutcome.Insufficient, result.Outcome);
            Assert.AreEqual(3, result.Snapshot.RenewableAmount);
            Assert.AreEqual(2, result.Snapshot.BonusAmount);
            Assert.IsFalse(result.Snapshot.ActiveRun.HasActiveRun);
        }

        [Test]
        public void TryStartRun_DuplicateRunId_ReturnsIdempotentReplay_NoDoubleSpend()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            int changedCount = 0;
            service.Changed += _ => changedCount++;

            EnergyRunStartResult first = service.TryStartRun("run-1");
            EnergyRunStartResult second = service.TryStartRun("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, first.Outcome);
            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, second.Outcome);
            Assert.AreEqual(first.IsFreeRun, second.IsFreeRun);
            Assert.AreEqual(2, second.Snapshot.RenewableAmount);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void TryStartRun_ConflictingActiveRun_RejectsDifferentRunId_OriginalUntouched()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));

            EnergyRunStartResult first = service.TryStartRun("run-A");
            EnergyRunStartResult second = service.TryStartRun("run-B");

            Assert.AreEqual(EnergyResultOutcome.Success, first.Outcome);
            Assert.AreEqual(EnergyResultOutcome.Conflict, second.Outcome);
            Assert.IsTrue(second.Snapshot.ActiveRun.HasActiveRun);
            Assert.AreEqual("run-A", second.Snapshot.ActiveRun.RunId);
        }

        [Test]
        public void MarkRunEntered_CorrectRunId_Succeeds_AndBlocksLaterCancellation()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");

            EnergyRunEntryResult entry = service.MarkRunEntered("run-1");
            Assert.AreEqual(EnergyResultOutcome.Success, entry.Outcome);

            EnergyRunCancellationResult cancel = service.CancelFailedStart("run-1");
            Assert.AreEqual(EnergyResultOutcome.Conflict, cancel.Outcome);
        }

        [Test]
        public void MarkRunEntered_MismatchedRunId_ReturnsConflict()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");

            EnergyRunEntryResult entry = service.MarkRunEntered("run-does-not-exist");
            Assert.AreEqual(EnergyResultOutcome.Conflict, entry.Outcome);
        }

        [Test]
        public void MarkRunEntered_CalledTwice_SecondIsIdempotentReplay()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");

            service.MarkRunEntered("run-1");
            EnergyRunEntryResult second = service.MarkRunEntered("run-1");

            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, second.Outcome);
        }

        [Test]
        public void CompleteRun_Win_Success_ClearsActiveRun_FreesSlotForNewRun()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            service.MarkRunEntered("run-1");

            EnergyRunCompletionResult result = service.CompleteRun("run-1", RunOutcome.Win);

            Assert.AreEqual(EnergyResultOutcome.Success, result.Outcome);
            Assert.AreEqual(RunOutcome.Win, result.RecordedOutcome);

            EnergyRunStartResult newRun = service.TryStartRun("run-2");
            Assert.AreEqual(EnergyResultOutcome.Success, newRun.Outcome);
        }

        [Test]
        public void CompleteRun_LossAndAbandoned_RecordCorrectOutcome()
        {
            EnergyService lossService = MakeService(out _, out _, MakeConfig(runCost: 1));
            lossService.TryStartRun("run-loss");
            EnergyRunCompletionResult lossResult = lossService.CompleteRun("run-loss", RunOutcome.Loss);
            Assert.AreEqual(EnergyResultOutcome.Success, lossResult.Outcome);
            Assert.AreEqual(RunOutcome.Loss, lossResult.RecordedOutcome);

            EnergyService abandonService = MakeService(out _, out _, MakeConfig(runCost: 1));
            abandonService.TryStartRun("run-abandon");
            EnergyRunCompletionResult abandonResult = abandonService.CompleteRun("run-abandon", RunOutcome.Abandoned);
            Assert.AreEqual(EnergyResultOutcome.Success, abandonResult.Outcome);
            Assert.AreEqual(RunOutcome.Abandoned, abandonResult.RecordedOutcome);
        }

        [Test]
        public void CompleteRun_RepeatedSameOutcome_ReturnsIdempotentReplay_NoBalanceChange()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            EnergyRunCompletionResult first = service.CompleteRun("run-1", RunOutcome.Win);
            EnergySnapshot before = service.Current;

            EnergyRunCompletionResult replay = service.CompleteRun("run-1", RunOutcome.Win);

            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, replay.Outcome);
            Assert.AreEqual(RunOutcome.Win, replay.RecordedOutcome);
            Assert.AreEqual(before.RenewableAmount, service.Current.RenewableAmount);
            Assert.AreEqual(before.BonusAmount, service.Current.BonusAmount);
        }

        [Test]
        public void CompleteRun_DifferentOutcomeThanRecorded_ReturnsConflict_WithOriginalOutcome()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            service.CompleteRun("run-1", RunOutcome.Win);
            EnergySnapshot before = service.Current;

            EnergyRunCompletionResult conflict = service.CompleteRun("run-1", RunOutcome.Loss);

            Assert.AreEqual(EnergyResultOutcome.Conflict, conflict.Outcome);
            Assert.AreEqual(RunOutcome.Win, conflict.RecordedOutcome);
            Assert.AreEqual(before.RenewableAmount, service.Current.RenewableAmount);
            Assert.AreEqual(before.BonusAmount, service.Current.BonusAmount);
        }

        [Test]
        public void CancelFailedStart_BeforeEntry_RestoresExactReservedAmounts()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 4, initialRenewable: 3, initialBonus: 5));
            EnergyRunStartResult started = service.TryStartRun("run-1");
            Assert.AreEqual(0, started.Snapshot.RenewableAmount);
            Assert.AreEqual(4, started.Snapshot.BonusAmount);

            EnergyRunCancellationResult cancel = service.CancelFailedStart("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, cancel.Outcome);
            Assert.AreEqual(3, cancel.Snapshot.RenewableAmount);
            Assert.AreEqual(5, cancel.Snapshot.BonusAmount);
        }

        [Test]
        public void CancelFailedStart_FreeRun_NoBalanceChange()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.GrantUnlimited(TimeSpan.FromMinutes(10), "tx-unlimited");
            service.TryStartRun("run-1");

            EnergyRunCancellationResult cancel = service.CancelFailedStart("run-1");

            Assert.AreEqual(EnergyResultOutcome.Success, cancel.Outcome);
            Assert.AreEqual(3, cancel.Snapshot.RenewableAmount);
            Assert.AreEqual(2, cancel.Snapshot.BonusAmount);
        }

        [Test]
        public void CancelFailedStart_AfterMarkRunEntered_ReturnsConflict_NoBalanceChange()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            service.MarkRunEntered("run-1");
            EnergySnapshot before = service.Current;

            EnergyRunCancellationResult cancel = service.CancelFailedStart("run-1");

            Assert.AreEqual(EnergyResultOutcome.Conflict, cancel.Outcome);
            Assert.AreEqual(before.RenewableAmount, service.Current.RenewableAmount);
            Assert.IsTrue(service.Current.ActiveRun.HasActiveRun);
        }

        [Test]
        public void CancelFailedStart_AfterCompleteRun_ReturnsConflict()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            service.CompleteRun("run-1", RunOutcome.Win);

            EnergyRunCancellationResult cancel = service.CancelFailedStart("run-1");

            Assert.AreEqual(EnergyResultOutcome.Conflict, cancel.Outcome);
        }

        [Test]
        public void CancelFailedStart_RepeatedForSameRunId_ReturnsIdempotentReplay()
        {
            EnergyService service = MakeService(out _, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            service.CancelFailedStart("run-1");

            EnergyRunCancellationResult second = service.CancelFailedStart("run-1");

            Assert.AreEqual(EnergyResultOutcome.IdempotentReplay, second.Outcome);
        }

        [Test]
        public void Restart_PendingNotEntered_SurvivesAndCancelWorks()
        {
            EnergyConfig config = MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2);
            FakeClock clock = new FakeClock(BaseUtc);
            InMemoryEnergyStateStore store = new InMemoryEnergyStateStore();
            FixedEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());

            EnergyService first = new EnergyService(clock, store, provider);
            first.TryStartRun("run-1");

            EnergyService second = new EnergyService(clock, store, provider);
            Assert.IsTrue(second.Current.ActiveRun.HasActiveRun);
            Assert.IsFalse(second.Current.ActiveRun.EnteredGameplay);

            EnergyRunCancellationResult cancel = second.CancelFailedStart("run-1");
            Assert.AreEqual(EnergyResultOutcome.Success, cancel.Outcome);
            Assert.AreEqual(3, cancel.Snapshot.RenewableAmount);
            Assert.AreEqual(2, cancel.Snapshot.BonusAmount);
        }

        [Test]
        public void Restart_Entered_SurvivesAndCancelRejected()
        {
            EnergyConfig config = MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2);
            FakeClock clock = new FakeClock(BaseUtc);
            InMemoryEnergyStateStore store = new InMemoryEnergyStateStore();
            FixedEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, config.ComputeVersion());

            EnergyService first = new EnergyService(clock, store, provider);
            first.TryStartRun("run-1");
            first.MarkRunEntered("run-1");

            EnergyService second = new EnergyService(clock, store, provider);
            Assert.IsTrue(second.Current.ActiveRun.HasActiveRun);
            Assert.IsTrue(second.Current.ActiveRun.EnteredGameplay);

            EnergyRunCancellationResult cancel = second.CancelFailedStart("run-1");
            Assert.AreEqual(EnergyResultOutcome.Conflict, cancel.Outcome);
        }

        [Test]
        public void PersistenceFailure_EachRunMethod_LeavesStateUnchanged()
        {
            EnergyService service = MakeService(out _, out InMemoryEnergyStateStore store, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            int changedCount = 0;
            service.Changed += _ => changedCount++;

            // TryStartRun failure.
            store.FailNextSave = true;
            EnergyRunStartResult startFail = service.TryStartRun("run-1");
            Assert.AreEqual(EnergyResultOutcome.PersistenceFailure, startFail.Outcome);
            Assert.IsFalse(service.Current.ActiveRun.HasActiveRun);
            Assert.AreEqual(3, service.Current.RenewableAmount);
            Assert.AreEqual(0, changedCount);

            // Successful start for subsequent failure tests.
            service.TryStartRun("run-1");
            Assert.AreEqual(1, changedCount);

            // MarkRunEntered failure.
            store.FailNextSave = true;
            EnergyRunEntryResult entryFail = service.MarkRunEntered("run-1");
            Assert.AreEqual(EnergyResultOutcome.PersistenceFailure, entryFail.Outcome);
            Assert.IsFalse(service.Current.ActiveRun.EnteredGameplay);
            Assert.AreEqual(1, changedCount);

            service.MarkRunEntered("run-1");
            Assert.AreEqual(2, changedCount);

            // CancelFailedStart failure (should be Conflict since entered — use CompleteRun failure instead).
            store.FailNextSave = true;
            EnergyRunCompletionResult completeFail = service.CompleteRun("run-1", RunOutcome.Win);
            Assert.AreEqual(EnergyResultOutcome.PersistenceFailure, completeFail.Outcome);
            Assert.IsTrue(service.Current.ActiveRun.HasActiveRun);
            Assert.AreEqual(2, changedCount);

            EnergyRunCompletionResult completeOk = service.CompleteRun("run-1", RunOutcome.Win);
            Assert.AreEqual(EnergyResultOutcome.Success, completeOk.Outcome);
            Assert.AreEqual(3, changedCount);
        }

        [Test]
        public void PersistenceFailure_CancelFailedStart_LeavesStateUnchanged()
        {
            EnergyService service = MakeService(out _, out InMemoryEnergyStateStore store, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.TryStartRun("run-1");
            int changedCount = 0;
            service.Changed += _ => changedCount++;

            store.FailNextSave = true;
            EnergyRunCancellationResult cancelFail = service.CancelFailedStart("run-1");

            Assert.AreEqual(EnergyResultOutcome.PersistenceFailure, cancelFail.Outcome);
            Assert.IsTrue(service.Current.ActiveRun.HasActiveRun);
            Assert.AreEqual(2, service.Current.RenewableAmount);
            Assert.AreEqual(0, changedCount);

            EnergyRunCancellationResult cancelOk = service.CancelFailedStart("run-1");
            Assert.AreEqual(EnergyResultOutcome.Success, cancelOk.Outcome);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void FreeRun_UnlimitedExpiresMidRun_CompletesAsFree_NoBalanceDeducted()
        {
            EnergyService service = MakeService(out FakeClock clock, out _, MakeConfig(runCost: 1, initialRenewable: 3, initialBonus: 2));
            service.GrantUnlimited(TimeSpan.FromMinutes(1), "tx-unlimited");

            EnergyRunStartResult started = service.TryStartRun("run-1");
            Assert.IsTrue(started.IsFreeRun);

            clock.Advance(TimeSpan.FromMinutes(5)); // unlimited expires mid-run

            EnergyRunCompletionResult completed = service.CompleteRun("run-1", RunOutcome.Win);

            Assert.AreEqual(EnergyResultOutcome.Success, completed.Outcome);
            Assert.AreEqual(3, completed.Snapshot.RenewableAmount);
            Assert.AreEqual(2, completed.Snapshot.BonusAmount);
        }
    }
}

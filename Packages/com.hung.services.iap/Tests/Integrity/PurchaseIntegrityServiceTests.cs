using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Hung.Base;
using Hung.Base.Persistence;

namespace Hung.IAP.Tests
{
    public sealed class PurchaseIntegrityServiceTests
    {
        [Test]
        public async Task PurchaseAsync_PersistsObservedBeforeValidation()
        {
            var rig = Rig();
            rig.Store.NextObserved = StoreRecord("tx-1");

            await rig.Service.PurchaseAsync(new PurchaseProductId("starter-pack"));

            CollectionAssert.AreEqual(
                new[] { "record:tx-1:Observed", "validate:tx-1", "state:tx-1:Validated", "state:tx-1:GrantPending", "grant:tx-1", "state:tx-1:Granted", "confirm:tx-1", "state:tx-1:Completed" },
                rig.Log);
        }

        [Test]
        public async Task PurchaseAsync_PersistsGrantPendingBeforeGrant()
        {
            var rig = Rig();
            rig.Store.NextObserved = StoreRecord("tx-1");

            await rig.Service.PurchaseAsync(new PurchaseProductId("starter-pack"));

            Assert.That(rig.Log.IndexOf("state:tx-1:GrantPending"), Is.LessThan(rig.Log.IndexOf("grant:tx-1")));
        }

        [Test]
        public async Task CompletedDuplicate_SkipsValidatorGrantAndConfirm()
        {
            var rig = Rig();
            rig.Ledger.CompletedTransactions.Add("tx-1");

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord("tx-1"), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.Completed));
            Assert.That(rig.Validator.CallCount, Is.EqualTo(0));
            Assert.That(rig.Grant.CallCount, Is.EqualTo(0));
            Assert.That(rig.Store.ConfirmCount, Is.EqualTo(0));
        }

        [Test]
        public async Task AlreadyGranted_ContinuesToConfirm()
        {
            var rig = Rig();
            rig.Grant.NextStatus = PurchaseGrantStatus.AlreadyGranted;

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord("tx-1"), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.Completed));
            Assert.That(rig.Log, Does.Contain("state:tx-1:Granted"));
            Assert.That(rig.Log, Does.Contain("confirm:tx-1"));
        }

        [Test]
        public async Task RetryableGrant_RemainsResumableAndDoesNotConfirm()
        {
            var rig = Rig();
            rig.Grant.NextStatus = PurchaseGrantStatus.RetryableFailure;

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord("tx-1"), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.RetryableFailure));
            Assert.That(rig.Log, Does.Contain("state:tx-1:GrantRetryable"));
            Assert.That(rig.Log, Does.Not.Contain("confirm:tx-1"));
        }

        [Test]
        public async Task RetryableValidation_RemainsResumableAndDoesNotGrant()
        {
            var rig = Rig();
            rig.Validator.NextResult = PurchaseValidationResult.RetryableFailure("validator-offline");

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord("tx-1"), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.RetryableFailure));
            Assert.That(rig.Log, Does.Contain("state:tx-1:ValidationRetryable"));
            Assert.That(rig.Grant.CallCount, Is.EqualTo(0));
            Assert.That(rig.Store.ConfirmCount, Is.EqualTo(0));
        }

        [Test]
        public async Task LedgerConflict_FailsClosedBeforeValidation()
        {
            var rig = Rig();
            rig.Ledger.NextRecordStatus = PurchaseLedgerWriteStatus.Conflict;

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord("tx-1"), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo(PurchaseLedgerCodes.TransactionConflict));
            Assert.That(rig.Validator.CallCount, Is.EqualTo(0));
            Assert.That(rig.Grant.CallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task RetryableConfirmation_PersistsRetryAndDoesNotGrantAgain()
        {
            var rig = Rig();
            rig.Store.NextConfirmation = StoreConfirmationResult.RetryableFailure("store-offline");

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord("tx-1"), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.RetryableFailure));
            Assert.That(rig.Grant.CallCount, Is.EqualTo(1));
            Assert.That(rig.Store.ConfirmCount, Is.EqualTo(1));
            Assert.That(rig.Log, Does.Contain("state:tx-1:ConfirmationRetryable"));
        }

        [Test]
        public async Task MissingTransactionId_NeverGrantsOrConfirms()
        {
            var rig = Rig();

            PurchaseRequestResult result = await rig.Service.ProcessObservedForTestsAsync(StoreRecord(null), PurchaseSource.Redelivery);

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.Rejected));
            Assert.That(rig.Log, Does.Not.Contain("grant:"));
            Assert.That(rig.Log, Does.Not.Contain("confirm:"));
        }

        [Test]
        public async Task SubscriberException_IsIsolated()
        {
            var rig = Rig();
            rig.Store.NextObserved = StoreRecord("tx-1");
            rig.Service.TransactionUpdated += _ => throw new InvalidOperationException("subscriber failed");

            PurchaseRequestResult result = await rig.Service.PurchaseAsync(new PurchaseProductId("starter-pack"));

            Assert.That(result.Status, Is.EqualTo(PurchaseRequestStatus.Completed));
            Assert.That(rig.Diagnostics.Codes, Does.Contain(PurchaseIntegrityCodes.SubscriberFailed));
        }

        [Test]
        public async Task ConcurrentDuplicateCallback_JoinsOneProcessingTask()
        {
            var rig = Rig();
            StorePurchaseRecord record = StoreRecord("tx-1");

            Task<PurchaseRequestResult> first = rig.Service.ProcessObservedForTestsAsync(record, PurchaseSource.Redelivery);
            Task<PurchaseRequestResult> second = rig.Service.ProcessObservedForTestsAsync(record, PurchaseSource.Redelivery);
            await Task.WhenAll(first, second);

            Assert.That(rig.Validator.CallCount, Is.EqualTo(1));
            Assert.That(rig.Grant.CallCount, Is.EqualTo(1));
            Assert.That(rig.Store.ConfirmCount, Is.EqualTo(1));
            Assert.That(first.Result.Status, Is.EqualTo(PurchaseRequestStatus.Completed));
            Assert.That(second.Result.Status, Is.EqualTo(PurchaseRequestStatus.Completed));
        }

        private static TestRig Rig()
        {
            var log = new List<string>();
            var catalog = new PurchaseCatalog(new[]
            {
                new PurchaseCatalogEntry(
                    new PurchaseProductId("starter-pack"),
                    PurchaseProductType.Consumable,
                    "google-starter",
                    "apple-starter",
                    "editor-starter",
                    true,
                    1)
            });
            var store = new FakeStoreAdapter(log);
            var validator = new FakePurchaseValidator(log);
            var ledger = new FakePurchaseLedger(log);
            var grant = new FakeGrantHandler(log);
            var diagnostics = new FakePurchaseDiagnostics();

            return new TestRig(
                log,
                store,
                validator,
                ledger,
                grant,
                diagnostics,
                new PurchaseIntegrityService(catalog, store, validator, ledger, grant, diagnostics));
        }

        private static StorePurchaseRecord StoreRecord(string transactionId)
        {
            return new StorePurchaseRecord(
                transactionId,
                PurchaseStoreNames.GooglePlay,
                "google-starter",
                "receipt-json",
                "fingerprint",
                PurchaseProductType.Consumable);
        }

        private sealed class TestRig
        {
            public TestRig(
                List<string> log,
                FakeStoreAdapter store,
                FakePurchaseValidator validator,
                FakePurchaseLedger ledger,
                FakeGrantHandler grant,
                FakePurchaseDiagnostics diagnostics,
                PurchaseIntegrityService service)
            {
                Log = log;
                Store = store;
                Validator = validator;
                Ledger = ledger;
                Grant = grant;
                Diagnostics = diagnostics;
                Service = service;
            }

            public List<string> Log { get; }
            public FakeStoreAdapter Store { get; }
            public FakePurchaseValidator Validator { get; }
            public FakePurchaseLedger Ledger { get; }
            public FakeGrantHandler Grant { get; }
            public FakePurchaseDiagnostics Diagnostics { get; }
            public PurchaseIntegrityService Service { get; }
        }

        private sealed class FakeStoreAdapter : IPurchaseStoreAdapter
        {
            private readonly List<string> log;

            public FakeStoreAdapter(List<string> log) => this.log = log;

            public PurchaseAvailability Availability { get; set; } = new PurchaseAvailability(PurchaseCapabilityState.Ready);
            public StorePurchaseRecord NextObserved { get; set; }
            public StoreConfirmationResult NextConfirmation { get; set; } = StoreConfirmationResult.Success();
            public int ConfirmCount { get; private set; }
            public event Action<StorePurchaseRecord> PurchaseObserved;

            public Task ConnectAsync(CancellationToken token) => Task.CompletedTask;

            public Task<StoreRequestResult> BeginPurchaseAsync(string storeProductId, CancellationToken token)
            {
                return Task.FromResult(StoreRequestResult.Observed(NextObserved));
            }

            public Task<IReadOnlyList<StorePurchaseRecord>> FetchPurchasesAsync(CancellationToken token)
            {
                return Task.FromResult<IReadOnlyList<StorePurchaseRecord>>(Array.Empty<StorePurchaseRecord>());
            }

            public Task<StoreRestoreResult> RestoreAsync(CancellationToken token)
            {
                return Task.FromResult(StoreRestoreResult.Success(Array.Empty<StorePurchaseRecord>()));
            }

            public Task<StoreConfirmationResult> ConfirmAsync(string transactionId, CancellationToken token)
            {
                ConfirmCount++;
                log.Add("confirm:" + transactionId);
                return Task.FromResult(NextConfirmation);
            }

            public void Emit(StorePurchaseRecord record) => PurchaseObserved?.Invoke(record);
        }

        private sealed class FakePurchaseValidator : IPurchaseValidator
        {
            private readonly List<string> log;

            public FakePurchaseValidator(List<string> log) => this.log = log;

            public int CallCount { get; private set; }
            public PurchaseValidationResult NextResult { get; set; } = PurchaseValidationResult.Valid("{}");

            public Task<PurchaseValidationResult> ValidateAsync(StorePurchaseRecord record, PurchaseCatalogEntry entry, CancellationToken token)
            {
                CallCount++;
                log.Add("validate:" + record.TransactionId);
                return Task.FromResult(NextResult);
            }
        }

        private sealed class FakeGrantHandler : IPurchaseGrantHandler
        {
            private readonly List<string> log;

            public FakeGrantHandler(List<string> log) => this.log = log;

            public PurchaseGrantStatus NextStatus { get; set; } = PurchaseGrantStatus.Granted;
            public int CallCount { get; private set; }

            public Task<PurchaseGrantStatus> GrantAsync(PurchaseGrantRequest request, CancellationToken token = default)
            {
                CallCount++;
                log.Add("grant:" + request.TransactionId);
                return Task.FromResult(NextStatus);
            }
        }

        private sealed class FakePurchaseLedger : IPurchaseLedger
        {
            private readonly List<string> log;
            private readonly PurchaseLedgerState state = new PurchaseLedgerState();

            public FakePurchaseLedger(List<string> log) => this.log = log;

            public HashSet<string> CompletedTransactions { get; } = new HashSet<string>(StringComparer.Ordinal);
            public PurchaseLedgerWriteStatus NextRecordStatus { get; set; } = PurchaseLedgerWriteStatus.Saved;
            public bool IsAvailable { get; set; } = true;
            public SaveRecoveryState LoadRecovery => SaveRecoveryState.None;
            public PurchaseLedgerState State => state;

            public PurchaseLedgerWriteResult RecordObserved(PurchaseTransactionRecord record)
            {
                if (CompletedTransactions.Contains(record.transactionId))
                {
                    log.Add("duplicate-completed:" + record.transactionId);
                    return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Duplicate, null, record);
                }
                if (NextRecordStatus == PurchaseLedgerWriteStatus.Conflict)
                    return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Conflict, PurchaseLedgerCodes.TransactionConflict, record);

                log.Add("record:" + record.transactionId + ":" + record.state);
                state.transactions.Add(record);
                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Saved, null, record);
            }

            public PurchaseLedgerWriteResult UpdateState(string transactionId, PurchaseTransactionState nextState, string code = null)
            {
                log.Add("state:" + transactionId + ":" + nextState);
                return new PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus.Saved);
            }

            public bool ContainsCompletedTransaction(string transactionId) => CompletedTransactions.Contains(transactionId);
        }

        private sealed class FakePurchaseDiagnostics : IPurchaseDiagnostics
        {
            public List<string> Codes { get; } = new List<string>();
            public void Report(string code, string transactionId = null, string message = null) => Codes.Add(code);
        }
    }
}

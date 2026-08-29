using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Hung.IAP.PlayModeTests
{
    public sealed class PurchaseRecoveryPlayModeTests
    {
        private string root;
        private SaveDefinition<PurchaseLedgerState> definition;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath, "ComHungPurchaseRecoveryTests", Guid.NewGuid().ToString("N"));
            definition = PurchaseLedgerDefinition.Create(new PlainJsonSaveCodec(), new Sha256SaveProtector());
        }

        [TearDown]
        public void TearDown()
        {
            string fullRoot = Path.GetFullPath(root);
            string allowedRoot = Path.GetFullPath(Path.Combine(Application.temporaryCachePath, "ComHungPurchaseRecoveryTests"));
            if (fullRoot.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
                Directory.Delete(fullRoot, true);
        }

        [TestCase(PurchaseTransactionState.ValidationPending)]
        [TestCase(PurchaseTransactionState.GrantPending)]
        public async Task Reconcile_AfterRestart_CompletesPendingTransaction(PurchaseTransactionState crashState)
        {
            SeedLedger(crashState);
            var grant = new RecordingGrantHandler();
            PurchaseIntegrityService service = CreateIntegrityService(grant);

            PurchaseReconcileResult result = await service.ReconcileAsync();

            Assert.That(result.Status, Is.EqualTo(PurchaseAggregateStatus.Success));
            Assert.That(grant.MutationCount, Is.EqualTo(1));
            Assert.That(LoadLedger().ContainsCompletedTransaction("tx-1"), Is.True);
        }

        [TestCase(PurchaseTransactionState.Granted)]
        [TestCase(PurchaseTransactionState.StoreConfirmationPending)]
        public async Task Reconcile_AfterRestart_AlreadyGrantedTransactionOnlyConfirms(PurchaseTransactionState crashState)
        {
            SeedLedger(crashState);
            var grant = new RecordingGrantHandler();
            grant.MarkAlreadyGranted("tx-1");
            PurchaseIntegrityService service = CreateIntegrityService(grant);

            PurchaseReconcileResult result = await service.ReconcileAsync();

            Assert.That(result.Status, Is.EqualTo(PurchaseAggregateStatus.Success));
            Assert.That(grant.MutationCount, Is.EqualTo(0));
            Assert.That(LoadLedger().ContainsCompletedTransaction("tx-1"), Is.True);
        }

        [Test]
        public async Task Restore_AfterRestart_CompletesRedeliveredEntitlement()
        {
            SeedLedger(PurchaseTransactionState.GrantPending);
            var grant = new RecordingGrantHandler();
            FakeStore store = StoreWithRecord();
            PurchaseIntegrityService service = CreateIntegrityService(grant, store);

            PurchaseRestoreResult result = await service.RestoreAsync();

            Assert.That(result.Status, Is.EqualTo(PurchaseAggregateStatus.Success));
            Assert.That(store.ConfirmCount, Is.EqualTo(1));
            Assert.That(grant.MutationCount, Is.EqualTo(1));
            Assert.That(LoadLedger().ContainsCompletedTransaction("tx-1"), Is.True);
        }

        [Test]
        public void LedgerLoad_CorruptPrimaryUsesBackup()
        {
            SeedLedger(PurchaseTransactionState.Observed);
            SeedLedger(PurchaseTransactionState.Completed);
            File.WriteAllText(PrimaryPath(), "corrupt-primary");

            PersistencePurchaseLedger ledger = LoadLedger();

            Assert.That(ledger.IsAvailable, Is.True);
            Assert.That(ledger.LoadRecovery, Is.EqualTo(SaveRecoveryState.BackupRestored));
            Assert.That(ledger.State.transactions, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Reconcile_UnrecoverableLedger_DoesNotGrant()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrimaryPath()));
            File.WriteAllText(PrimaryPath(), "corrupt-primary");
            var grant = new RecordingGrantHandler();
            PurchaseIntegrityService service = CreateIntegrityService(grant);

            PurchaseReconcileResult result = await service.ReconcileAsync();

            Assert.That(result.Status, Is.EqualTo(PurchaseAggregateStatus.PartialSuccess));
            Assert.That(grant.MutationCount, Is.EqualTo(0));
        }

        private PurchaseIntegrityService CreateIntegrityService(RecordingGrantHandler grant, FakeStore store = null)
        {
            return new PurchaseIntegrityService(
                Catalog(),
                store ?? StoreWithRecord(),
                new AlwaysValidValidator(),
                LoadLedger(),
                grant,
                null);
        }

        private PersistencePurchaseLedger LoadLedger()
        {
            return PersistencePurchaseLedger.Load(CreatePersistence(), definition);
        }

        private void SeedLedger(PurchaseTransactionState state)
        {
            PurchaseLedgerState ledgerState = new PurchaseLedgerState();
            ledgerState.transactions.Add(new PurchaseTransactionRecord
            {
                transactionId = "tx-1",
                productId = "starter-pack",
                storeName = PurchaseStoreNames.GooglePlay,
                storeProductId = "google-starter",
                productType = PurchaseProductType.Consumable,
                source = PurchaseSource.NewPurchase,
                state = state,
                firstObservedUtcTicks = 10,
                lastUpdatedUtcTicks = 10,
                receiptFingerprintSha256 = "fingerprint-1",
                catalogVersion = 1
            });

            SaveResult save = CreatePersistence().Save(definition, ledgerState);
            Assert.That(save.Success, Is.True, save.DiagnosticCode);
        }

        private PersistenceService CreatePersistence()
        {
            return new PersistenceService(new FileSaveStore(root));
        }

        private string PrimaryPath()
        {
            return Path.Combine(root, "primary", PurchaseLedgerDefinition.Key + ".save");
        }

        private static PurchaseCatalog Catalog()
        {
            return new PurchaseCatalog(new[]
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
        }

        private static FakeStore StoreWithRecord()
        {
            return new FakeStore(Record());
        }

        private static StorePurchaseRecord Record()
        {
            return new StorePurchaseRecord(
                "tx-1",
                PurchaseStoreNames.GooglePlay,
                "google-starter",
                "receipt",
                "fingerprint-1",
                PurchaseProductType.Consumable);
        }

        private sealed class FakeStore : IPurchaseStoreAdapter
        {
            private readonly StorePurchaseRecord record;

            public FakeStore(StorePurchaseRecord record)
            {
                this.record = record;
            }

            public int ConfirmCount { get; private set; }

            public PurchaseAvailability Availability => new PurchaseAvailability(PurchaseCapabilityState.Ready);

            public event Action<StorePurchaseRecord> PurchaseObserved;

            public Task ConnectAsync(CancellationToken token) => Task.CompletedTask;

            public Task<StoreRequestResult> BeginPurchaseAsync(string storeProductId, CancellationToken token)
            {
                return Task.FromResult(StoreRequestResult.Observed(record));
            }

            public Task<IReadOnlyList<StorePurchaseRecord>> FetchPurchasesAsync(CancellationToken token)
            {
                return Task.FromResult<IReadOnlyList<StorePurchaseRecord>>(new[] { record });
            }

            public Task<StoreRestoreResult> RestoreAsync(CancellationToken token)
            {
                return Task.FromResult(StoreRestoreResult.Success(new[] { record }));
            }

            public Task<StoreConfirmationResult> ConfirmAsync(string transactionId, CancellationToken token)
            {
                ConfirmCount++;
                return Task.FromResult(StoreConfirmationResult.Success());
            }
        }

        private sealed class AlwaysValidValidator : IPurchaseValidator
        {
            public Task<PurchaseValidationResult> ValidateAsync(StorePurchaseRecord record, PurchaseCatalogEntry entry, CancellationToken token)
            {
                return Task.FromResult(PurchaseValidationResult.Valid("{\"source\":\"test\"}"));
            }
        }

        private sealed class RecordingGrantHandler : IPurchaseGrantHandler
        {
            private readonly HashSet<string> alreadyGranted = new HashSet<string>(StringComparer.Ordinal);

            public int MutationCount { get; private set; }

            public void MarkAlreadyGranted(string transactionId)
            {
                alreadyGranted.Add(transactionId);
            }

            public Task<PurchaseGrantStatus> GrantAsync(PurchaseGrantRequest request, CancellationToken token = default)
            {
                if (alreadyGranted.Contains(request.TransactionId))
                    return Task.FromResult(PurchaseGrantStatus.AlreadyGranted);

                MutationCount++;
                alreadyGranted.Add(request.TransactionId);
                return Task.FromResult(PurchaseGrantStatus.Granted);
            }
        }
    }
}

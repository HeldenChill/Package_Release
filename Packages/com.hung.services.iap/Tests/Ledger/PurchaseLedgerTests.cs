using System;
using System.Collections.Generic;
using NUnit.Framework;
using Hung.Base;
using Hung.Base.Persistence;

namespace Hung.IAP.Tests
{
    public sealed class PurchaseLedgerTests
    {
        [Test]
        public void Load_WhenMissing_CreatesEmptyAvailableLedger()
        {
            var service = new FakePersistenceService();

            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            Assert.That(ledger.IsAvailable, Is.True);
            Assert.That(ledger.State.transactions, Is.Empty);
            Assert.That(ledger.State.schemaVersion, Is.EqualTo(1));
        }

        [Test]
        public void RecordObserved_RoundTripsThroughPersistence()
        {
            var service = new FakePersistenceService();
            var definition = PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance);
            var ledger = PersistencePurchaseLedger.Load(service, definition);

            var result = ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1"));

            Assert.That(result.Status, Is.EqualTo(PurchaseLedgerWriteStatus.Saved));
            Assert.That(service.SavedValue.transactions, Has.Count.EqualTo(1));

            service.LoadResult = LoadResultExtensions.Success(service.SavedValue);
            var loaded = PersistencePurchaseLedger.Load(service, definition);

            Assert.That(loaded.State.transactions[0].transactionId, Is.EqualTo("tx-1"));
            Assert.That(loaded.State.transactions[0].state, Is.EqualTo(PurchaseTransactionState.Observed));
        }

        [Test]
        public void RecordObserved_DuplicateIdentity_DoesNotAppend()
        {
            var service = new FakePersistenceService();
            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            Assert.That(ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1")).Status, Is.EqualTo(PurchaseLedgerWriteStatus.Saved));
            var duplicate = ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1"));

            Assert.That(duplicate.Status, Is.EqualTo(PurchaseLedgerWriteStatus.Duplicate));
            Assert.That(ledger.State.transactions, Has.Count.EqualTo(1));
        }

        [TestCase("other-pack", "google-starter", "fingerprint-1")]
        [TestCase("starter-pack", "other-store-id", "fingerprint-1")]
        [TestCase("starter-pack", "google-starter", "other-fingerprint")]
        public void RecordObserved_ConflictingIdentity_FailsClosed(string productId, string storeProductId, string fingerprint)
        {
            var service = new FakePersistenceService();
            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1"));
            var conflict = ledger.RecordObserved(Record("tx-1", productId, storeProductId, fingerprint));

            Assert.That(conflict.Status, Is.EqualTo(PurchaseLedgerWriteStatus.Conflict));
            Assert.That(conflict.Code, Is.EqualTo(PurchaseLedgerCodes.TransactionConflict));
            Assert.That(ledger.State.transactions, Has.Count.EqualTo(1));
        }

        [Test]
        public void SaveFailure_DoesNotPublishObservedMutation()
        {
            var service = new FakePersistenceService();
            service.NextSaveSucceeds = false;
            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            var result = ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1"));

            Assert.That(result.Status, Is.EqualTo(PurchaseLedgerWriteStatus.PersistenceFailed));
            Assert.That(ledger.State.transactions, Is.Empty);
        }

        [Test]
        public void SaveFailure_DoesNotPublishStateMutation()
        {
            var service = new FakePersistenceService();
            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));
            ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1"));

            service.NextSaveSucceeds = false;
            var result = ledger.UpdateState("tx-1", PurchaseTransactionState.GrantPending, "grant-pending");

            Assert.That(result.Status, Is.EqualTo(PurchaseLedgerWriteStatus.PersistenceFailed));
            Assert.That(ledger.State.transactions[0].state, Is.EqualTo(PurchaseTransactionState.Observed));
        }

        [Test]
        public void Load_FromValidBackup_RemainsAvailable()
        {
            var service = new FakePersistenceService
            {
                LoadResult = new LoadResult<PurchaseLedgerState>(
                    true,
                    StateWith(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1")),
                    null,
                    SaveDataSource.Backup,
                    SaveRecoveryState.BackupRestored)
            };

            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            Assert.That(ledger.IsAvailable, Is.True);
            Assert.That(ledger.LoadRecovery, Is.EqualTo(SaveRecoveryState.BackupRestored));
            Assert.That(ledger.State.transactions, Has.Count.EqualTo(1));
        }

        [TestCase(SaveRecoveryState.Unrecoverable)]
        [TestCase(SaveRecoveryState.UnsupportedNewerVersion)]
        public void Load_UnrecoverableOrNewerSchema_FailsClosed(SaveRecoveryState recovery)
        {
            var service = new FakePersistenceService
            {
                LoadResult = new LoadResult<PurchaseLedgerState>(
                    false,
                    null,
                    "LOAD_FAILED",
                    SaveDataSource.Primary,
                    recovery)
            };

            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            Assert.That(ledger.IsAvailable, Is.False);
            Assert.That(ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1")).Status, Is.EqualTo(PurchaseLedgerWriteStatus.Unavailable));
        }

        [Test]
        public void CompletedTransactionIdentity_IsRetainedIndefinitely()
        {
            var service = new FakePersistenceService();
            var ledger = PersistencePurchaseLedger.Load(service, PurchaseLedgerDefinition.Create(TestCodec.Instance, TestProtector.Instance));

            ledger.RecordObserved(Record("tx-1", "starter-pack", "google-starter", "fingerprint-1"));
            ledger.UpdateState("tx-1", PurchaseTransactionState.Completed, "complete");

            Assert.That(ledger.ContainsCompletedTransaction("tx-1"), Is.True);
            Assert.That(ledger.State.transactions, Has.Count.EqualTo(1));
        }

        private static PurchaseTransactionRecord Record(string transactionId, string productId, string storeProductId, string fingerprint)
        {
            return new PurchaseTransactionRecord
            {
                transactionId = transactionId,
                productId = productId,
                storeName = PurchaseStoreNames.GooglePlay,
                storeProductId = storeProductId,
                productType = PurchaseProductType.Consumable,
                source = PurchaseSource.NewPurchase,
                state = PurchaseTransactionState.Observed,
                receiptFingerprintSha256 = fingerprint,
                firstObservedUtcTicks = 10,
                lastUpdatedUtcTicks = 10,
                catalogVersion = 1
            };
        }

        private static PurchaseLedgerState StateWith(PurchaseTransactionRecord record)
        {
            var state = new PurchaseLedgerState();
            state.transactions.Add(record);
            return state;
        }

        private sealed class FakePersistenceService : IPersistenceService
        {
            public LoadResult<PurchaseLedgerState> LoadResult = LoadResultExtensions.Success(new PurchaseLedgerState());
            public PurchaseLedgerState SavedValue;
            public bool NextSaveSucceeds = true;

            public SaveResult Save<T>(SaveDefinition<T> definition, T value) where T : new()
            {
                if (!NextSaveSucceeds)
                {
                    NextSaveSucceeds = true;
                    return new SaveResult(false, "SAVE_FAILED");
                }

                SavedValue = value as PurchaseLedgerState;
                LoadResult = new LoadResult<PurchaseLedgerState>(true, SavedValue, null, SaveDataSource.Primary, SaveRecoveryState.None);
                return new SaveResult(true);
            }

            public LoadResult<T> Load<T>(SaveDefinition<T> definition) where T : new()
            {
                object result = LoadResult;
                return (LoadResult<T>)result;
            }

            public void Register<T>(SaveDefinition<T> definition) where T : new()
            {
            }

            public bool TryGetDefinition<T>(string requestedKey, out SaveDefinition<T> definition) where T : new()
            {
                definition = null;
                return false;
            }
        }

        private sealed class TestCodec : ISaveCodec
        {
            public static readonly TestCodec Instance = new TestCodec();
            public string EncodingId => "test";
            public SaveEncodedPayload Encode(byte[] jsonBytes) => new SaveEncodedPayload(EncodingId, jsonBytes);
            public byte[] Decode(SaveEncodedPayload payload) => payload.Bytes;
        }

        private sealed class TestProtector : ISaveProtector
        {
            public static readonly TestProtector Instance = new TestProtector();
            public string ProtectionId => "test";
            public string Protect(byte[] authenticatedBytes) => "test";
            public bool Verify(byte[] authenticatedBytes, string tag) => tag == "test";
        }
    }

    internal static class LoadResultExtensions
    {
        public static LoadResult<T> Success<T>(T value)
        {
            return new LoadResult<T>(true, value, null, SaveDataSource.Default, SaveRecoveryState.DefaultCreated);
        }
    }
}

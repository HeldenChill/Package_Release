using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hung.Base;

namespace Hung.IAP
{
    public sealed class PurchaseIntegrityService : IPurchaseIntegrityService
    {
        private readonly IPurchaseCatalogProvider catalog;
        private readonly IPurchaseStoreAdapter store;
        private readonly IPurchaseValidator validator;
        private readonly IPurchaseLedger ledger;
        private readonly IPurchaseGrantHandler grantHandler;
        private readonly IPurchaseDiagnostics diagnostics;
        private readonly PurchaseProcessingLock processingLock = new PurchaseProcessingLock();
        private readonly SemaphoreSlim interactiveRequestGate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource serviceLifetime = new CancellationTokenSource();

        public PurchaseIntegrityService(
            IPurchaseCatalogProvider catalog,
            IPurchaseStoreAdapter store,
            IPurchaseValidator validator,
            IPurchaseLedger ledger,
            IPurchaseGrantHandler grantHandler,
            IPurchaseDiagnostics diagnostics)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.grantHandler = grantHandler ?? throw new ArgumentNullException(nameof(grantHandler));
            this.diagnostics = diagnostics;
            this.store.PurchaseObserved += OnPurchaseObserved;
        }

        public PurchaseAvailability Availability => ledger.IsAvailable ? store.Availability : new PurchaseAvailability(PurchaseCapabilityState.Misconfigured, PurchaseIntegrityCodes.LedgerUnavailable);

        public event Action<PurchaseTransactionSnapshot> TransactionUpdated;

        public async Task<PurchaseAvailability> ConnectAsync(CancellationToken token = default)
        {
            if (!ledger.IsAvailable)
                return Availability;

            await store.ConnectAsync(token).ConfigureAwait(false);
            return Availability;
        }

        public async Task<PurchaseRequestResult> PurchaseAsync(PurchaseProductId productId, CancellationToken token = default)
        {
            if (!ledger.IsAvailable)
                return new PurchaseRequestResult(PurchaseRequestStatus.Misconfigured, PurchaseIntegrityCodes.LedgerUnavailable);
            if (!catalog.TryGet(productId, out PurchaseCatalogEntry entry))
                return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, PurchaseIntegrityCodes.ProductNotFound);
            if (!Availability.CanPurchase)
                return new PurchaseRequestResult(PurchaseRequestStatus.Unsupported, Availability.Code);

            await interactiveRequestGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                string storeProductId = StoreProductIdFor(entry);
                if (string.IsNullOrEmpty(storeProductId))
                    return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, PurchaseIntegrityCodes.StoreProductNotFound);

                StoreRequestResult request = await store.BeginPurchaseAsync(storeProductId, token).ConfigureAwait(false);
                switch (request.Status)
                {
                    case StoreRequestStatus.Observed:
                        return await ProcessObservedAsync(request.ObservedPurchase, PurchaseSource.NewPurchase, serviceLifetime.Token).ConfigureAwait(false);
                    case StoreRequestStatus.Cancelled:
                        return new PurchaseRequestResult(PurchaseRequestStatus.Cancelled, request.Code);
                    case StoreRequestStatus.Deferred:
                        return new PurchaseRequestResult(PurchaseRequestStatus.Deferred, request.Code);
                    case StoreRequestStatus.Failed:
                        return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, request.Code);
                    default:
                        return new PurchaseRequestResult(PurchaseRequestStatus.Pending, request.Code);
                }
            }
            finally
            {
                interactiveRequestGate.Release();
            }
        }

        public async Task<PurchaseReconcileResult> ReconcileAsync(CancellationToken token = default)
        {
            IReadOnlyList<StorePurchaseRecord> purchases = await store.FetchPurchasesAsync(token).ConfigureAwait(false);
            var snapshots = new List<PurchaseTransactionSnapshot>();
            bool hadFailure = false;

            foreach (StorePurchaseRecord purchase in purchases)
            {
                PurchaseRequestResult result = await ProcessObservedAsync(purchase, PurchaseSource.Redelivery, serviceLifetime.Token).ConfigureAwait(false);
                snapshots.Add(result.Transaction);
                hadFailure |= !result.IsCompleted;
            }

            PurchaseAggregateStatus status = snapshots.Count == 0
                ? PurchaseAggregateStatus.NothingToProcess
                : hadFailure ? PurchaseAggregateStatus.PartialSuccess : PurchaseAggregateStatus.Success;
            return new PurchaseReconcileResult(status, null, snapshots);
        }

        public async Task<PurchaseRestoreResult> RestoreAsync(CancellationToken token = default)
        {
            StoreRestoreResult restore = await store.RestoreAsync(token).ConfigureAwait(false);
            if (!restore.IsSuccess)
                return new PurchaseRestoreResult(PurchaseAggregateStatus.RetryableFailure, restore.Code, Array.Empty<PurchaseTransactionSnapshot>());

            var snapshots = new List<PurchaseTransactionSnapshot>();
            bool hadFailure = false;
            foreach (StorePurchaseRecord purchase in restore.Purchases)
            {
                PurchaseRequestResult result = await ProcessObservedAsync(purchase, PurchaseSource.Restore, serviceLifetime.Token).ConfigureAwait(false);
                snapshots.Add(result.Transaction);
                hadFailure |= !result.IsCompleted;
            }

            PurchaseAggregateStatus status = snapshots.Count == 0
                ? PurchaseAggregateStatus.NothingToProcess
                : hadFailure ? PurchaseAggregateStatus.PartialSuccess : PurchaseAggregateStatus.Success;
            return new PurchaseRestoreResult(status, null, snapshots);
        }

        public Task<PurchaseRequestResult> ProcessObservedForTestsAsync(StorePurchaseRecord record, PurchaseSource source)
        {
            return ProcessObservedAsync(record, source, serviceLifetime.Token);
        }

        private Task<PurchaseRequestResult> ProcessObservedAsync(StorePurchaseRecord record, PurchaseSource source, CancellationToken token)
        {
            if (string.IsNullOrEmpty(record.TransactionId))
            {
                diagnostics?.Report(PurchaseIntegrityCodes.MissingTransactionId);
                return Task.FromResult(new PurchaseRequestResult(PurchaseRequestStatus.Rejected, PurchaseIntegrityCodes.MissingTransactionId));
            }

            return processingLock.RunOrJoin(record.TransactionId, () => ProcessObservedCoreAsync(record, source, token));
        }

        private async Task<PurchaseRequestResult> ProcessObservedCoreAsync(StorePurchaseRecord record, PurchaseSource source, CancellationToken token)
        {
            if (ledger.ContainsCompletedTransaction(record.TransactionId))
                return CompletedDuplicate(record);

            if (!catalog.TryResolveStoreId(record.StoreName, record.StoreProductId, out PurchaseCatalogEntry entry))
                return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, PurchaseIntegrityCodes.StoreProductNotFound);

            var observedRecord = new PurchaseTransactionRecord
            {
                transactionId = record.TransactionId,
                productId = entry.ProductId.Value,
                storeName = record.StoreName,
                storeProductId = record.StoreProductId,
                productType = entry.Type,
                source = source,
                state = PurchaseTransactionState.Observed,
                firstObservedUtcTicks = DateTime.UtcNow.Ticks,
                lastUpdatedUtcTicks = DateTime.UtcNow.Ticks,
                receiptFingerprintSha256 = record.ReceiptFingerprintSha256,
                catalogVersion = entry.CatalogVersion
            };

            PurchaseLedgerWriteResult observed = ledger.RecordObserved(observedRecord);
            if (observed.Status == PurchaseLedgerWriteStatus.Conflict)
                return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, observed.Code);
            if (observed.Status == PurchaseLedgerWriteStatus.Unavailable || observed.Status == PurchaseLedgerWriteStatus.PersistenceFailed)
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, observed.Code);
            if (observed.Status == PurchaseLedgerWriteStatus.Duplicate && ledger.ContainsCompletedTransaction(record.TransactionId))
                return CompletedDuplicate(record);

            Publish(Snapshot(observedRecord));

            PurchaseValidationResult validation = await validator.ValidateAsync(record, entry, token).ConfigureAwait(false);
            if (validation.Status == PurchaseValidationStatus.RetryableFailure)
            {
                ledger.UpdateState(record.TransactionId, PurchaseTransactionState.ValidationRetryable, validation.Code);
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, validation.Code, Snapshot(observedRecord));
            }
            if (validation.Status != PurchaseValidationStatus.Valid)
            {
                ledger.UpdateState(record.TransactionId, PurchaseTransactionState.Rejected, validation.Code);
                return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, validation.Code, Snapshot(observedRecord));
            }

            PurchaseLedgerWriteResult validated = ledger.UpdateState(record.TransactionId, PurchaseTransactionState.Validated, "validated");
            if (validated.Status != PurchaseLedgerWriteStatus.Saved)
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, validated.Code, Snapshot(observedRecord));

            PurchaseLedgerWriteResult grantPending = ledger.UpdateState(record.TransactionId, PurchaseTransactionState.GrantPending, "grant-pending");
            if (grantPending.Status != PurchaseLedgerWriteStatus.Saved)
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, grantPending.Code, Snapshot(observedRecord));

            PurchaseGrantStatus grant = await grantHandler.GrantAsync(
                new PurchaseGrantRequest(
                    record.TransactionId,
                    entry.ProductId,
                    entry.Type,
                    source,
                    record.StoreName,
                    record.StoreProductId,
                    validation.MetadataJson),
                token).ConfigureAwait(false);

            if (grant == PurchaseGrantStatus.RetryableFailure)
            {
                ledger.UpdateState(record.TransactionId, PurchaseTransactionState.GrantRetryable, "grant-retryable");
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, "grant-retryable", Snapshot(observedRecord));
            }
            if (grant == PurchaseGrantStatus.PermanentFailure || grant == PurchaseGrantStatus.CancelledBeforeMutation)
            {
                ledger.UpdateState(record.TransactionId, PurchaseTransactionState.GrantRejected, "grant-rejected");
                return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, "grant-rejected", Snapshot(observedRecord));
            }

            PurchaseLedgerWriteResult granted = ledger.UpdateState(record.TransactionId, PurchaseTransactionState.Granted, "granted");
            if (granted.Status != PurchaseLedgerWriteStatus.Saved)
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, granted.Code, Snapshot(observedRecord));

            StoreConfirmationResult confirmation = await store.ConfirmAsync(record.TransactionId, token).ConfigureAwait(false);
            if (!confirmation.IsSuccess)
            {
                PurchaseTransactionState next = confirmation.Retryable ? PurchaseTransactionState.ConfirmationRetryable : PurchaseTransactionState.StoreConfirmationPending;
                ledger.UpdateState(record.TransactionId, next, confirmation.Code ?? PurchaseIntegrityCodes.ConfirmationRetryable);
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, confirmation.Code, Snapshot(observedRecord));
            }

            ledger.UpdateState(record.TransactionId, PurchaseTransactionState.Completed, "completed");
            return new PurchaseRequestResult(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED", Snapshot(observedRecord));
        }

        private void OnPurchaseObserved(StorePurchaseRecord record)
        {
            _ = ProcessObservedAsync(record, PurchaseSource.Redelivery, serviceLifetime.Token);
        }

        private PurchaseRequestResult CompletedDuplicate(StorePurchaseRecord record)
        {
            return new PurchaseRequestResult(
                PurchaseRequestStatus.Completed,
                "PURCHASE_COMPLETED",
                new PurchaseTransactionSnapshot(
                    record.TransactionId,
                    default,
                    record.StoreName,
                    record.StoreProductId,
                    record.ProductType,
                    PurchaseSource.Redelivery,
                    PurchaseTransactionState.Completed.ToString()));
        }

        private void Publish(PurchaseTransactionSnapshot snapshot)
        {
            Action<PurchaseTransactionSnapshot> handlers = TransactionUpdated;
            if (handlers == null)
                return;

            foreach (Action<PurchaseTransactionSnapshot> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(snapshot);
                }
                catch (Exception ex)
                {
                    diagnostics?.Report(PurchaseIntegrityCodes.SubscriberFailed, snapshot.TransactionId, ex.Message);
                }
            }
        }

        private static PurchaseTransactionSnapshot Snapshot(PurchaseTransactionRecord record)
        {
            PurchaseProductId id = default;
            if (!string.IsNullOrEmpty(record.productId))
                id = new PurchaseProductId(record.productId);

            return new PurchaseTransactionSnapshot(
                record.transactionId,
                id,
                record.storeName,
                record.storeProductId,
                record.productType,
                record.source,
                record.state.ToString(),
                record.errorCode);
        }

        private static string StoreProductIdFor(PurchaseCatalogEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.GooglePlayId))
                return entry.GooglePlayId;
            if (!string.IsNullOrEmpty(entry.AppleAppStoreId))
                return entry.AppleAppStoreId;
            return entry.EditorTestId;
        }
    }
}

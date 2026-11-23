using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hung.Base;

namespace Hung.IAP
{
    // Do not add ConfigureAwait(false) here. The store completes its tasks with
    // RunContinuationsAsynchronously, so without the captured Unity context every step after the
    // first await - ledger saves and the grant handler that mutates GameData - ran on a thread-pool thread.
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

            await store.ConnectAsync(token);
            return Availability;
        }

        // The store goes not-ready after a disconnect and nothing else reconnects it, so a purchase
        // request reconnects once. A failed connect leaves the request Unsupported.
        private async Task TryReconnectAsync(CancellationToken token)
        {
            try
            {
                await store.ConnectAsync(token);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                diagnostics?.Report(PurchaseIntegrityCodes.ReconnectFailed, null, ex.Message);
            }
        }

        public async Task<PurchaseRequestResult> PurchaseAsync(PurchaseProductId productId, CancellationToken token = default)
        {
            if (!ledger.IsAvailable)
                return new PurchaseRequestResult(PurchaseRequestStatus.Misconfigured, PurchaseIntegrityCodes.LedgerUnavailable);
            if (!catalog.TryGet(productId, out PurchaseCatalogEntry entry))
                return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, PurchaseIntegrityCodes.ProductNotFound);
            if (!Availability.CanPurchase && Availability.State == PurchaseCapabilityState.SupportedNotReady)
                await TryReconnectAsync(token);
            if (!Availability.CanPurchase)
                return new PurchaseRequestResult(PurchaseRequestStatus.Unsupported, Availability.Code);

            await interactiveRequestGate.WaitAsync(token);
            try
            {
                string storeProductId = StoreProductIdFor(entry);
                if (string.IsNullOrEmpty(storeProductId))
                    return new PurchaseRequestResult(PurchaseRequestStatus.Rejected, PurchaseIntegrityCodes.StoreProductNotFound);

                StoreRequestResult request = await store.BeginPurchaseAsync(storeProductId, token);
                switch (request.Status)
                {
                    case StoreRequestStatus.Observed:
                        // An order for another product (fetched pending order, approved Ask to Buy) is not
                        // this request's answer. The store delivers it through PurchaseObserved as well.
                        if (!string.Equals(request.ObservedPurchase.StoreProductId, storeProductId, StringComparison.Ordinal))
                            return new PurchaseRequestResult(PurchaseRequestStatus.Pending, PurchaseIntegrityCodes.StoreOrderMismatch);
                        return await ProcessObservedAsync(request.ObservedPurchase, PurchaseSource.NewPurchase, serviceLifetime.Token);
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
            IReadOnlyList<StorePurchaseRecord> purchases = await store.FetchPurchasesAsync(token);
            var snapshots = new List<PurchaseTransactionSnapshot>();
            bool hadFailure = false;

            foreach (StorePurchaseRecord purchase in purchases)
            {
                PurchaseRequestResult result = await ProcessObservedAsync(purchase, PurchaseSource.Redelivery, serviceLifetime.Token);
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
            StoreRestoreResult restore = await store.RestoreAsync(token);
            if (!restore.IsSuccess)
                return new PurchaseRestoreResult(PurchaseAggregateStatus.RetryableFailure, restore.Code, Array.Empty<PurchaseTransactionSnapshot>());

            var snapshots = new List<PurchaseTransactionSnapshot>();
            bool hadFailure = false;
            foreach (StorePurchaseRecord purchase in restore.Purchases)
            {
                PurchaseRequestResult result = await ProcessObservedAsync(purchase, PurchaseSource.Restore, serviceLifetime.Token);
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
                return await ConfirmCompletedDuplicateAsync(record, token);

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
                return await ConfirmCompletedDuplicateAsync(record, token);
            // Granted but not confirmed earlier: confirm only. Never validate or grant the same transaction twice,
            // so a grant handler without its own dedupe cannot double-grant on redelivery or restore.
            if (observed.Status == PurchaseLedgerWriteStatus.Duplicate && IsGrantedNotCompleted(observed.Record))
                return await ConfirmAndCompleteAsync(record, observedRecord, token);

            Publish(Snapshot(observedRecord));

            PurchaseValidationResult validation = await validator.ValidateAsync(record, entry, token);
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
                token);

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

            return await ConfirmAndCompleteAsync(record, observedRecord, token);
        }

        private static bool IsGrantedNotCompleted(PurchaseTransactionRecord existing)
        {
            return existing != null &&
                   (existing.state == PurchaseTransactionState.Granted ||
                    existing.state == PurchaseTransactionState.ConfirmationRetryable ||
                    existing.state == PurchaseTransactionState.StoreConfirmationPending);
        }

        private async Task<PurchaseRequestResult> ConfirmAndCompleteAsync(StorePurchaseRecord record, PurchaseTransactionRecord observedRecord, CancellationToken token)
        {
            StoreConfirmationResult confirmation = await store.ConfirmAsync(record.TransactionId, token);
            if (!confirmation.IsSuccess)
            {
                PurchaseTransactionState next = confirmation.Retryable ? PurchaseTransactionState.ConfirmationRetryable : PurchaseTransactionState.StoreConfirmationPending;
                ledger.UpdateState(record.TransactionId, next, confirmation.Code ?? PurchaseIntegrityCodes.ConfirmationRetryable);
                return new PurchaseRequestResult(PurchaseRequestStatus.RetryableFailure, confirmation.Code, Snapshot(observedRecord));
            }

            ledger.UpdateState(record.TransactionId, PurchaseTransactionState.Completed, "completed");
            return new PurchaseRequestResult(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED", Snapshot(observedRecord));
        }

        // async void is the event boundary: nothing awaits a redelivery, so failures are reported here
        // instead of vanishing in a dropped task. The ledger keeps the record resumable.
        private async void OnPurchaseObserved(StorePurchaseRecord record)
        {
            try
            {
                await ProcessObservedAsync(record, PurchaseSource.Redelivery, serviceLifetime.Token);
            }
            catch (Exception ex)
            {
                diagnostics?.Report(PurchaseIntegrityCodes.ObservedProcessingFailed, record.TransactionId, ex.Message);
            }
        }

        // The ledger marks Completed once ConfirmAsync reports success, but the store may still fail the
        // consume or acknowledge afterwards. A Completed transaction that the store delivers again is
        // therefore confirmed once more (never validated or granted again); an already finished order
        // makes this a no-op.
        private async Task<PurchaseRequestResult> ConfirmCompletedDuplicateAsync(StorePurchaseRecord record, CancellationToken token)
        {
            try
            {
                StoreConfirmationResult confirmation = await store.ConfirmAsync(record.TransactionId, token);
                if (!confirmation.IsSuccess)
                    diagnostics?.Report(confirmation.Code ?? PurchaseIntegrityCodes.ConfirmationRetryable, record.TransactionId, "re-confirm of a completed transaction");
            }
            catch (Exception ex)
            {
                diagnostics?.Report(PurchaseIntegrityCodes.ConfirmationRetryable, record.TransactionId, ex.Message);
            }

            return CompletedDuplicate(record);
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

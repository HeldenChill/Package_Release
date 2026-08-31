using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hung.Base;
using UnityEngine.Purchasing;

namespace Hung.IAP
{
    public sealed class UnityPurchaseStoreAdapter : IPurchaseStoreAdapter
    {
        private readonly StoreController controller;
        private readonly string storeName;
        private readonly Dictionary<string, PendingOrder> pendingOrdersByTransactionId = new Dictionary<string, PendingOrder>(StringComparer.Ordinal);
        private TaskCompletionSource<StoreRequestResult> activePurchase;
        private TaskCompletionSource<IReadOnlyList<StorePurchaseRecord>> activeFetch;
        private bool connected;

        public UnityPurchaseStoreAdapter(StoreController controller, string storeName)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.storeName = string.IsNullOrEmpty(storeName) ? PurchaseStoreNames.GooglePlay : storeName;

            controller.OnPurchasePending += OnPurchasePending;
            controller.OnPurchaseFailed += OnPurchaseFailed;
            controller.OnPurchaseDeferred += OnPurchaseDeferred;
            controller.OnPurchasesFetched += OnPurchasesFetched;
            controller.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            controller.OnStoreDisconnected += _ => connected = false;
        }

        public PurchaseAvailability Availability => connected
            ? new PurchaseAvailability(PurchaseCapabilityState.Ready)
            : new PurchaseAvailability(PurchaseCapabilityState.SupportedNotReady, "UNITY_PURCHASING_NOT_CONNECTED");

        public event Action<StorePurchaseRecord> PurchaseObserved;

        public async Task ConnectAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await controller.Connect().ConfigureAwait(false);
            connected = true;
        }

        public Task<StoreRequestResult> BeginPurchaseAsync(string storeProductId, CancellationToken token)
        {
            activePurchase = new TaskCompletionSource<StoreRequestResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (token.CanBeCanceled)
                token.Register(() => activePurchase.TrySetCanceled());

            controller.PurchaseProduct(storeProductId);
            return activePurchase.Task;
        }

        public Task<IReadOnlyList<StorePurchaseRecord>> FetchPurchasesAsync(CancellationToken token)
        {
            activeFetch = new TaskCompletionSource<IReadOnlyList<StorePurchaseRecord>>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (token.CanBeCanceled)
                token.Register(() => activeFetch.TrySetCanceled());

            controller.FetchPurchases();
            return activeFetch.Task;
        }

        public async Task<StoreRestoreResult> RestoreAsync(CancellationToken token)
        {
            var restore = new TaskCompletionSource<StoreRestoreResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (token.CanBeCanceled)
                token.Register(() => restore.TrySetCanceled());

            controller.RestoreTransactions((success, message) =>
            {
                if (!success)
                    restore.TrySetResult(StoreRestoreResult.Failed(message));
                else
                    restore.TrySetResult(StoreRestoreResult.Success(Array.Empty<StorePurchaseRecord>()));
            });

            return await restore.Task.ConfigureAwait(false);
        }

        public Task<StoreConfirmationResult> ConfirmAsync(string transactionId, CancellationToken token)
        {
            if (string.IsNullOrEmpty(transactionId) || !pendingOrdersByTransactionId.TryGetValue(transactionId, out PendingOrder pendingOrder))
                return Task.FromResult(StoreConfirmationResult.RetryableFailure("UNITY_PENDING_ORDER_NOT_FOUND"));

            token.ThrowIfCancellationRequested();
            controller.ConfirmPurchase(pendingOrder);
            return Task.FromResult(StoreConfirmationResult.Success());
        }

        private void OnPurchasePending(PendingOrder order)
        {
            UnityOrderSnapshot snapshot = UnityOrderTranslator.FromUnityOrder(order, UnityOrderKind.Pending, storeName);
            StoreRequestResult result = UnityOrderTranslator.TranslateRequestResult(snapshot);
            if (result.Status == StoreRequestStatus.Observed)
            {
                pendingOrdersByTransactionId[result.ObservedPurchase.TransactionId] = order;
                PurchaseObserved?.Invoke(result.ObservedPurchase);
            }

            activePurchase?.TrySetResult(result);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            activePurchase?.TrySetResult(UnityOrderTranslator.TranslateRequestResult(UnityOrderTranslator.FromUnityFailedOrder(order)));
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            UnityOrderSnapshot snapshot = UnityOrderTranslator.FromUnityOrder(order, UnityOrderKind.Deferred, storeName);
            activePurchase?.TrySetResult(UnityOrderTranslator.TranslateRequestResult(snapshot));
        }

        private void OnPurchasesFetched(Orders orders)
        {
            var records = new List<StorePurchaseRecord>();
            AddRecords(records, orders.PendingOrders, UnityOrderKind.Pending);
            AddRecords(records, orders.ConfirmedOrders, UnityOrderKind.Confirmed);
            activeFetch?.TrySetResult(records);
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            activeFetch?.TrySetException(new InvalidOperationException(failure.Message));
        }

        private void AddRecords<TOrder>(List<StorePurchaseRecord> records, IReadOnlyList<TOrder> orders, UnityOrderKind kind) where TOrder : Order
        {
            if (orders == null)
                return;

            foreach (TOrder order in orders)
            {
                UnityOrderSnapshot snapshot = UnityOrderTranslator.FromUnityOrder(order, kind, storeName);
                if (UnityOrderTranslator.TryTranslatePurchase(snapshot, out StorePurchaseRecord record, out _))
                    records.Add(record);
            }
        }
    }
}

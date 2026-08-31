using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hung.Base;

namespace Hung.IAP
{
    public interface IPurchaseStoreAdapter
    {
        PurchaseAvailability Availability { get; }

        Task ConnectAsync(CancellationToken token);

        Task<StoreRequestResult> BeginPurchaseAsync(string storeProductId, CancellationToken token);

        Task<IReadOnlyList<StorePurchaseRecord>> FetchPurchasesAsync(CancellationToken token);

        Task<StoreRestoreResult> RestoreAsync(CancellationToken token);

        Task<StoreConfirmationResult> ConfirmAsync(string transactionId, CancellationToken token);

        event Action<StorePurchaseRecord> PurchaseObserved;
    }

    public readonly struct StorePurchaseRecord
    {
        public StorePurchaseRecord(
            string transactionId,
            string storeName,
            string storeProductId,
            string receipt,
            string receiptFingerprintSha256,
            PurchaseProductType productType)
        {
            TransactionId = transactionId;
            StoreName = storeName;
            StoreProductId = storeProductId;
            Receipt = receipt;
            ReceiptFingerprintSha256 = receiptFingerprintSha256;
            ProductType = productType;
        }

        public string TransactionId { get; }
        public string StoreName { get; }
        public string StoreProductId { get; }
        public string Receipt { get; }
        public string ReceiptFingerprintSha256 { get; }
        public PurchaseProductType ProductType { get; }
    }

    public enum StoreRequestStatus
    {
        Started,
        Observed,
        Cancelled,
        Deferred,
        Failed
    }

    public readonly struct StoreRequestResult
    {
        private StoreRequestResult(StoreRequestStatus status, string code, StorePurchaseRecord observedPurchase)
        {
            Status = status;
            Code = code;
            ObservedPurchase = observedPurchase;
        }

        public StoreRequestStatus Status { get; }
        public string Code { get; }
        public StorePurchaseRecord ObservedPurchase { get; }

        public static StoreRequestResult Started() => new StoreRequestResult(StoreRequestStatus.Started, null, default);
        public static StoreRequestResult Observed(StorePurchaseRecord record) => new StoreRequestResult(StoreRequestStatus.Observed, null, record);
        public static StoreRequestResult Cancelled(string code = null) => new StoreRequestResult(StoreRequestStatus.Cancelled, code, default);
        public static StoreRequestResult Deferred(string code = null) => new StoreRequestResult(StoreRequestStatus.Deferred, code, default);
        public static StoreRequestResult Failed(string code) => new StoreRequestResult(StoreRequestStatus.Failed, code, default);
    }

    public readonly struct StoreConfirmationResult
    {
        private StoreConfirmationResult(bool success, bool retryable, string code)
        {
            IsSuccess = success;
            Retryable = retryable;
            Code = code;
        }

        public bool IsSuccess { get; }
        public bool Retryable { get; }
        public string Code { get; }

        public static StoreConfirmationResult Success() => new StoreConfirmationResult(true, false, null);
        public static StoreConfirmationResult RetryableFailure(string code) => new StoreConfirmationResult(false, true, code);
        public static StoreConfirmationResult Failed(string code) => new StoreConfirmationResult(false, false, code);
    }

    public readonly struct StoreRestoreResult
    {
        private StoreRestoreResult(bool success, string code, IReadOnlyList<StorePurchaseRecord> purchases)
        {
            IsSuccess = success;
            Code = code;
            Purchases = purchases ?? Array.Empty<StorePurchaseRecord>();
        }

        public bool IsSuccess { get; }
        public string Code { get; }
        public IReadOnlyList<StorePurchaseRecord> Purchases { get; }

        public static StoreRestoreResult Success(IReadOnlyList<StorePurchaseRecord> purchases) => new StoreRestoreResult(true, null, purchases);
        public static StoreRestoreResult Failed(string code) => new StoreRestoreResult(false, code, Array.Empty<StorePurchaseRecord>());
    }
}

using System;
using System.Collections.Generic;

namespace Hung.Base
{
    public enum PurchaseCapabilityState
    {
        Unsupported,
        SupportedNotReady,
        Ready,
        Misconfigured
    }

    public enum PurchaseProductType
    {
        Consumable,
        PermanentEntitlement,
        Subscription
    }

    public enum PurchaseSource
    {
        NewPurchase,
        Redelivery,
        Restore
    }

    public enum PurchaseGrantStatus
    {
        Granted,
        AlreadyGranted,
        RetryableFailure,
        PermanentFailure,
        CancelledBeforeMutation
    }

    public enum PurchaseRequestStatus
    {
        Completed,
        Pending,
        Deferred,
        Cancelled,
        Rejected,
        RetryableFailure,
        Unsupported,
        Misconfigured
    }

    public enum PurchaseAggregateStatus
    {
        Success,
        PartialSuccess,
        NothingToProcess,
        RetryableFailure,
        Failed,
        Unsupported,
        Misconfigured
    }

    public readonly struct PurchaseAvailability
    {
        public PurchaseAvailability(PurchaseCapabilityState state, string code = null)
        {
            State = state;
            Code = code;
        }

        public PurchaseCapabilityState State { get; }

        public string Code { get; }

        public bool CanPurchase => State == PurchaseCapabilityState.Ready;
    }

    public readonly struct PurchaseTransactionSnapshot
    {
        public PurchaseTransactionSnapshot(
            string transactionId,
            PurchaseProductId productId,
            string storeName,
            string storeProductId,
            PurchaseProductType productType,
            PurchaseSource source,
            string state,
            string code = null)
        {
            TransactionId = transactionId;
            ProductId = productId;
            StoreName = storeName;
            StoreProductId = storeProductId;
            ProductType = productType;
            Source = source;
            State = state;
            Code = code;
        }

        public string TransactionId { get; }

        public PurchaseProductId ProductId { get; }

        public string StoreName { get; }

        public string StoreProductId { get; }

        public PurchaseProductType ProductType { get; }

        public PurchaseSource Source { get; }

        public string State { get; }

        public string Code { get; }
    }

    public readonly struct PurchaseGrantRequest
    {
        public PurchaseGrantRequest(
            string transactionId,
            PurchaseProductId productId,
            PurchaseProductType productType,
            PurchaseSource source,
            string storeName,
            string storeProductId,
            string validationMetadataJson = null)
        {
            TransactionId = transactionId;
            ProductId = productId;
            ProductType = productType;
            Source = source;
            StoreName = storeName;
            StoreProductId = storeProductId;
            ValidationMetadataJson = validationMetadataJson;
        }

        public string TransactionId { get; }

        public PurchaseProductId ProductId { get; }

        public PurchaseProductType ProductType { get; }

        public PurchaseSource Source { get; }

        public string StoreName { get; }

        public string StoreProductId { get; }

        public string ValidationMetadataJson { get; }
    }

    public readonly struct PurchaseRequestResult
    {
        public PurchaseRequestResult(
            PurchaseRequestStatus status,
            string code,
            PurchaseTransactionSnapshot transaction = default)
        {
            Status = status;
            Code = code;
            Transaction = transaction;
        }

        public PurchaseRequestStatus Status { get; }

        public string Code { get; }

        public PurchaseTransactionSnapshot Transaction { get; }

        public bool IsCompleted => Status == PurchaseRequestStatus.Completed;
    }

    public readonly struct PurchaseReconcileResult
    {
        public PurchaseReconcileResult(
            PurchaseAggregateStatus status,
            string code,
            IReadOnlyList<PurchaseTransactionSnapshot> transactions)
        {
            Status = status;
            Code = code;
            Transactions = Copy(transactions);
        }

        public PurchaseAggregateStatus Status { get; }

        public string Code { get; }

        public IReadOnlyList<PurchaseTransactionSnapshot> Transactions { get; }

        private static IReadOnlyList<PurchaseTransactionSnapshot> Copy(IReadOnlyList<PurchaseTransactionSnapshot> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<PurchaseTransactionSnapshot>();

            var copy = new PurchaseTransactionSnapshot[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];

            return Array.AsReadOnly(copy);
        }
    }

    public readonly struct PurchaseRestoreResult
    {
        public PurchaseRestoreResult(
            PurchaseAggregateStatus status,
            string code,
            IReadOnlyList<PurchaseTransactionSnapshot> transactions)
        {
            Status = status;
            Code = code;
            Transactions = Copy(transactions);
        }

        public PurchaseAggregateStatus Status { get; }

        public string Code { get; }

        public IReadOnlyList<PurchaseTransactionSnapshot> Transactions { get; }

        private static IReadOnlyList<PurchaseTransactionSnapshot> Copy(IReadOnlyList<PurchaseTransactionSnapshot> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<PurchaseTransactionSnapshot>();

            var copy = new PurchaseTransactionSnapshot[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];

            return Array.AsReadOnly(copy);
        }
    }
}

using System;
using System.Collections.Generic;
using Hung.Base;

namespace Hung.IAP
{
    public enum PurchaseTransactionState
    {
        Observed,
        ValidationPending,
        ValidationRetryable,
        Rejected,
        Validated,
        GrantPending,
        GrantRetryable,
        GrantRejected,
        Granted,
        StoreConfirmationPending,
        ConfirmationRetryable,
        Completed
    }

    public enum PurchaseLedgerWriteStatus
    {
        Saved,
        Duplicate,
        NotFound,
        Conflict,
        PersistenceFailed,
        Unavailable
    }

    public static class PurchaseLedgerCodes
    {
        public const string TransactionConflict = "PURCHASE_LEDGER_TRANSACTION_CONFLICT";
        public const string SaveFailed = "PURCHASE_LEDGER_SAVE_FAILED";
        public const string Unavailable = "PURCHASE_LEDGER_UNAVAILABLE";
        public const string NotFound = "PURCHASE_LEDGER_TRANSACTION_NOT_FOUND";
    }

    [Serializable]
    public sealed class PurchaseLedgerState
    {
        public int schemaVersion = 1;
        public int catalogVersion;
        public List<PurchaseTransactionRecord> transactions = new();
    }

    [Serializable]
    public sealed class PurchaseTransactionRecord
    {
        public string transactionId;
        public string productId;
        public string storeName;
        public string storeProductId;
        public PurchaseProductType productType;
        public PurchaseSource source;
        public PurchaseTransactionState state;
        public long firstObservedUtcTicks;
        public long lastUpdatedUtcTicks;
        public string receiptFingerprintSha256;
        public string validationMetadataJson;
        public int validationAttemptCount;
        public int grantAttemptCount;
        public int confirmationAttemptCount;
        public string errorCode;
        public bool storeConfirmationRequired = true;
        public int catalogVersion;
    }

    public readonly struct PurchaseLedgerWriteResult
    {
        public PurchaseLedgerWriteResult(PurchaseLedgerWriteStatus status, string code = null, PurchaseTransactionRecord record = null)
        {
            Status = status;
            Code = code;
            Record = record;
        }

        public PurchaseLedgerWriteStatus Status { get; }

        public string Code { get; }

        public PurchaseTransactionRecord Record { get; }
    }
}

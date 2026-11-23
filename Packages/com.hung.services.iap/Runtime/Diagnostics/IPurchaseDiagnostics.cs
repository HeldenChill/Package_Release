namespace Hung.IAP
{
    public interface IPurchaseDiagnostics
    {
        void Report(string code, string transactionId = null, string message = null);
    }

    public static class PurchaseIntegrityCodes
    {
        public const string SubscriberFailed = "PURCHASE_SUBSCRIBER_FAILED";
        /// <summary>A store-redelivered purchase threw while processing; it stays resumable for the next reconcile.</summary>
        public const string ObservedProcessingFailed = "PURCHASE_OBSERVED_PROCESSING_FAILED";
        public const string MissingTransactionId = "PURCHASE_TRANSACTION_ID_MISSING";
        public const string ProductNotFound = "PURCHASE_PRODUCT_NOT_FOUND";
        public const string StoreProductNotFound = "PURCHASE_STORE_PRODUCT_NOT_FOUND";
        public const string LedgerUnavailable = "PURCHASE_LEDGER_UNAVAILABLE";
        public const string ConfirmationRetryable = "PURCHASE_CONFIRMATION_RETRYABLE";
        /// <summary>The store answered a purchase request with an order for another product; the request stays pending.</summary>
        public const string StoreOrderMismatch = "PURCHASE_STORE_ORDER_MISMATCH";
        /// <summary>A purchase request found the store not ready and the reconnect attempt failed.</summary>
        public const string ReconnectFailed = "PURCHASE_STORE_RECONNECT_FAILED";
    }
}

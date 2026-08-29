namespace Hung.IAP
{
    public interface IPurchaseDiagnostics
    {
        void Report(string code, string transactionId = null, string message = null);
    }

    public static class PurchaseIntegrityCodes
    {
        public const string SubscriberFailed = "PURCHASE_SUBSCRIBER_FAILED";
        public const string MissingTransactionId = "PURCHASE_TRANSACTION_ID_MISSING";
        public const string ProductNotFound = "PURCHASE_PRODUCT_NOT_FOUND";
        public const string StoreProductNotFound = "PURCHASE_STORE_PRODUCT_NOT_FOUND";
        public const string LedgerUnavailable = "PURCHASE_LEDGER_UNAVAILABLE";
        public const string ConfirmationRetryable = "PURCHASE_CONFIRMATION_RETRYABLE";
    }
}

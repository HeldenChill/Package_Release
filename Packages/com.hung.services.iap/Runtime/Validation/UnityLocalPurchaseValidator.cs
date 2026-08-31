using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hung.IAP
{
    public sealed class UnityLocalPurchaseValidator : IPurchaseValidator
    {
        private readonly IUnityReceiptValidationBackend backend;
        private readonly string expectedApplicationId;

        public UnityLocalPurchaseValidator(IUnityReceiptValidationBackend backend, string expectedApplicationId)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this.expectedApplicationId = expectedApplicationId ?? throw new ArgumentNullException(nameof(expectedApplicationId));
        }

        public Task<PurchaseValidationResult> ValidateAsync(StorePurchaseRecord record, PurchaseCatalogEntry entry, CancellationToken token)
        {
            if (!IsSupportedStore(record.StoreName))
                return Task.FromResult(PurchaseValidationResult.ConfigurationError(UnityReceiptValidationCodes.PlatformUnsupported));
            if (!backend.IsConfigured)
                return Task.FromResult(PurchaseValidationResult.ConfigurationError(backend.ConfigurationErrorCode ?? UnityReceiptValidationCodes.ConfigurationInvalid));
            if (string.IsNullOrEmpty(record.Receipt))
                return Task.FromResult(PurchaseValidationResult.Rejected(UnityReceiptValidationCodes.ReceiptMissing));

            try
            {
                UnityValidatedReceipt[] receipts = backend.Validate(record.Receipt);
                UnityValidatedReceipt? matchingProduct = null;

                foreach (UnityValidatedReceipt receipt in receipts)
                {
                    if (string.Equals(receipt.ProductId, record.StoreProductId, StringComparison.Ordinal))
                    {
                        matchingProduct = receipt;
                        break;
                    }
                }

                if (!matchingProduct.HasValue)
                    return Task.FromResult(PurchaseValidationResult.Rejected(UnityReceiptValidationCodes.ProductMismatch));

                UnityValidatedReceipt value = matchingProduct.Value;
                if (!string.Equals(value.ApplicationId, expectedApplicationId, StringComparison.Ordinal))
                    return Task.FromResult(PurchaseValidationResult.Rejected(UnityReceiptValidationCodes.ApplicationMismatch));
                if (!string.Equals(value.TransactionId, record.TransactionId, StringComparison.Ordinal))
                    return Task.FromResult(PurchaseValidationResult.Rejected(UnityReceiptValidationCodes.TransactionMismatch));

                return Task.FromResult(PurchaseValidationResult.Valid(ToMetadataJson(record, value)));
            }
            catch (UnityReceiptSecurityException)
            {
                return Task.FromResult(PurchaseValidationResult.Rejected(UnityReceiptValidationCodes.SignatureInvalid));
            }
            catch (UnityReceiptConfigurationException ex)
            {
                return Task.FromResult(PurchaseValidationResult.ConfigurationError(ex.Code ?? UnityReceiptValidationCodes.ConfigurationInvalid));
            }
            catch (Exception)
            {
                return Task.FromResult(PurchaseValidationResult.RetryableFailure(UnityReceiptValidationCodes.BackendRetryableFailure));
            }
        }

        private static bool IsSupportedStore(string storeName)
        {
            return string.Equals(storeName, PurchaseStoreNames.GooglePlay, StringComparison.Ordinal) ||
                   string.Equals(storeName, PurchaseStoreNames.AppleAppStore, StringComparison.Ordinal);
        }

        private static string ToMetadataJson(StorePurchaseRecord record, UnityValidatedReceipt receipt)
        {
            return "{" +
                   "\"store\":\"" + Escape(record.StoreName) + "\"," +
                   "\"productId\":\"" + Escape(receipt.ProductId) + "\"," +
                   "\"transactionId\":\"" + Escape(receipt.TransactionId) + "\"," +
                   "\"purchaseUtc\":\"" + Escape(receipt.PurchaseUtc.ToString("O")) + "\"," +
                   "\"receiptType\":\"" + Escape(receipt.ReceiptType) + "\"" +
                   "}";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

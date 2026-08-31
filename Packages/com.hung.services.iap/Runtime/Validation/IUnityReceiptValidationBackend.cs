using System;
using UnityEngine.Purchasing.Security;

namespace Hung.IAP
{
    public interface IUnityReceiptValidationBackend
    {
        bool IsConfigured { get; }

        string ConfigurationErrorCode { get; }

        UnityValidatedReceipt[] Validate(string unityReceipt);
    }

    public static class UnityReceiptTypes
    {
        public const string GooglePlay = "GooglePlay";
        public const string AppleAppStore = "AppleAppStore";
    }

    public static class UnityReceiptValidationCodes
    {
        public const string SignatureInvalid = "PURCHASE_RECEIPT_SIGNATURE_INVALID";
        public const string ApplicationMismatch = "PURCHASE_RECEIPT_APP_ID_MISMATCH";
        public const string ProductMismatch = "PURCHASE_RECEIPT_PRODUCT_MISMATCH";
        public const string TransactionMismatch = "PURCHASE_RECEIPT_TRANSACTION_MISMATCH";
        public const string PlatformUnsupported = "PURCHASE_RECEIPT_PLATFORM_UNSUPPORTED";
        public const string GeneratedTanglesMissing = "PURCHASE_VALIDATOR_GENERATED_TANGLES_MISSING";
        public const string ConfigurationInvalid = "PURCHASE_VALIDATOR_CONFIG_INVALID";
        public const string ReceiptMissing = "PURCHASE_RECEIPT_MISSING";
        public const string BackendRetryableFailure = "PURCHASE_VALIDATOR_BACKEND_RETRYABLE";
    }

    public readonly struct UnityValidatedReceipt
    {
        public UnityValidatedReceipt(
            string storeName,
            string productId,
            string transactionId,
            string applicationId,
            DateTime purchaseUtc,
            string receiptType)
        {
            StoreName = storeName;
            ProductId = productId;
            TransactionId = transactionId;
            ApplicationId = applicationId;
            PurchaseUtc = purchaseUtc.Kind == DateTimeKind.Utc ? purchaseUtc : purchaseUtc.ToUniversalTime();
            ReceiptType = receiptType;
        }

        public string StoreName { get; }
        public string ProductId { get; }
        public string TransactionId { get; }
        public string ApplicationId { get; }
        public DateTime PurchaseUtc { get; }
        public string ReceiptType { get; }
    }

    public sealed class UnityReceiptSecurityException : Exception
    {
        public UnityReceiptSecurityException(string message) : base(message)
        {
        }
    }

    public sealed class UnityReceiptConfigurationException : Exception
    {
        public UnityReceiptConfigurationException(string code, string message = null)
            : base(message ?? code)
        {
            Code = code;
        }

        public string Code { get; }
    }

    public sealed class UnityCrossPlatformReceiptValidationBackend : IUnityReceiptValidationBackend
    {
        private readonly CrossPlatformValidator validator;
        private readonly string applicationId;

        public UnityCrossPlatformReceiptValidationBackend(byte[] googlePlayPublicKey, byte[] appleRootCertificate, string applicationId)
        {
            this.applicationId = applicationId;
            try
            {
                validator = new CrossPlatformValidator(googlePlayPublicKey, appleRootCertificate, applicationId, applicationId);
                IsConfigured = true;
            }
            catch (Exception ex)
            {
                IsConfigured = false;
                ConfigurationErrorCode = UnityReceiptValidationCodes.ConfigurationInvalid;
                ConfigurationException = ex;
            }
        }

        public bool IsConfigured { get; }

        public string ConfigurationErrorCode { get; }

        public Exception ConfigurationException { get; }

        public UnityValidatedReceipt[] Validate(string unityReceipt)
        {
            if (!IsConfigured)
                throw new UnityReceiptConfigurationException(ConfigurationErrorCode);

            try
            {
                IPurchaseReceipt[] receipts = validator.Validate(unityReceipt);
                var normalized = new UnityValidatedReceipt[receipts.Length];
                for (int i = 0; i < receipts.Length; i++)
                    normalized[i] = Normalize(receipts[i]);

                return normalized;
            }
            catch (IAPSecurityException ex)
            {
                throw new UnityReceiptSecurityException(ex.Message);
            }
        }

        private UnityValidatedReceipt Normalize(IPurchaseReceipt receipt)
        {
            string receiptType = receipt is GooglePlayReceipt ? UnityReceiptTypes.GooglePlay : UnityReceiptTypes.AppleAppStore;
            string storeName = receiptType == UnityReceiptTypes.GooglePlay ? PurchaseStoreNames.GooglePlay : PurchaseStoreNames.AppleAppStore;
            string appId = applicationId;

            if (receipt is GooglePlayReceipt google && !string.IsNullOrEmpty(google.packageName))
                appId = google.packageName;

            return new UnityValidatedReceipt(
                storeName,
                receipt.productID,
                receipt.transactionID,
                appId,
                receipt.purchaseDate,
                receiptType);
        }
    }

    public static class UnityReceiptValidationBackendFactory
    {
        public static IUnityReceiptValidationBackend FromObfuscationData(byte[] googlePlayData, byte[] appleData, string applicationId)
        {
            bool hasGoogle = googlePlayData != null && googlePlayData.Length > 0;
            bool hasApple = appleData != null && appleData.Length > 0;

            if (!hasGoogle && !hasApple)
                return new MisconfiguredUnityReceiptValidationBackend(UnityReceiptValidationCodes.GeneratedTanglesMissing);

            return new UnityCrossPlatformReceiptValidationBackend(googlePlayData, appleData, applicationId);
        }
    }

    public sealed class MisconfiguredUnityReceiptValidationBackend : IUnityReceiptValidationBackend
    {
        public MisconfiguredUnityReceiptValidationBackend(string code)
        {
            ConfigurationErrorCode = code;
        }

        public bool IsConfigured => false;

        public string ConfigurationErrorCode { get; }

        public UnityValidatedReceipt[] Validate(string unityReceipt)
        {
            throw new UnityReceiptConfigurationException(ConfigurationErrorCode);
        }
    }
}

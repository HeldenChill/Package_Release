using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Hung.Base;

namespace Hung.IAP.Tests
{
    public sealed class UnityLocalPurchaseValidatorTests
    {
        [Test]
        public async Task ValidateAsync_RejectsSecurityFailure()
        {
            var backend = FakeBackend.Configured();
            backend.ThrowSecurity = true;
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", PurchaseStoreNames.GooglePlay, "google-starter"), Entry("google-starter"), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo(UnityReceiptValidationCodes.SignatureInvalid));
        }

        [Test]
        public async Task ValidateAsync_RejectsApplicationMismatch()
        {
            var backend = FakeBackend.Configured(Receipt("tx-1", "google-starter", "other.app", UnityReceiptTypes.GooglePlay));
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", PurchaseStoreNames.GooglePlay, "google-starter"), Entry("google-starter"), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo(UnityReceiptValidationCodes.ApplicationMismatch));
        }

        [Test]
        public async Task ValidateAsync_RejectsProductMismatch()
        {
            var backend = FakeBackend.Configured(Receipt("tx-1", "other-product", "com.test.game", UnityReceiptTypes.GooglePlay));
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", PurchaseStoreNames.GooglePlay, "google-starter"), Entry("google-starter"), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo(UnityReceiptValidationCodes.ProductMismatch));
        }

        [Test]
        public async Task ValidateAsync_RejectsTransactionMismatch()
        {
            var backend = FakeBackend.Configured(Receipt("other-tx", "google-starter", "com.test.game", UnityReceiptTypes.GooglePlay));
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", PurchaseStoreNames.GooglePlay, "google-starter"), Entry("google-starter"), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo(UnityReceiptValidationCodes.TransactionMismatch));
        }

        [Test]
        public async Task ValidateAsync_RejectsUnsupportedPlatform()
        {
            var backend = FakeBackend.Configured(Receipt("tx-1", "steam-starter", "com.test.game", "Steam"));
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", "Steam", "steam-starter"), Entry("steam-starter"), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.ConfigurationError));
            Assert.That(result.Code, Is.EqualTo(UnityReceiptValidationCodes.PlatformUnsupported));
        }

        [Test]
        public async Task ValidateAsync_MissingObfuscationDataIsConfigurationError()
        {
            var backend = FakeBackend.Misconfigured(UnityReceiptValidationCodes.GeneratedTanglesMissing);
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", PurchaseStoreNames.GooglePlay, "google-starter"), Entry("google-starter"), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.ConfigurationError));
            Assert.That(result.Code, Is.EqualTo(UnityReceiptValidationCodes.GeneratedTanglesMissing));
        }

        [TestCase(PurchaseStoreNames.GooglePlay, UnityReceiptTypes.GooglePlay, "google-starter")]
        [TestCase(PurchaseStoreNames.AppleAppStore, UnityReceiptTypes.AppleAppStore, "apple-starter")]
        public async Task ValidateAsync_ValidMobileReceiptReturnsNormalizedMetadata(string storeName, string receiptType, string storeProductId)
        {
            var backend = FakeBackend.Configured(Receipt("tx-1", storeProductId, "com.test.game", receiptType));
            var validator = new UnityLocalPurchaseValidator(backend, "com.test.game");

            PurchaseValidationResult result = await validator.ValidateAsync(Record("tx-1", storeName, storeProductId), Entry(storeProductId), default);

            Assert.That(result.Status, Is.EqualTo(PurchaseValidationStatus.Valid));
            StringAssert.Contains("\"store\":\"" + storeName + "\"", result.MetadataJson);
            StringAssert.Contains("\"productId\":\"" + storeProductId + "\"", result.MetadataJson);
            StringAssert.Contains("\"transactionId\":\"tx-1\"", result.MetadataJson);
            StringAssert.Contains("\"receiptType\":\"" + receiptType + "\"", result.MetadataJson);
        }

        private static StorePurchaseRecord Record(string transactionId, string storeName, string storeProductId)
        {
            return new StorePurchaseRecord(
                transactionId,
                storeName,
                storeProductId,
                "receipt-json",
                "fingerprint",
                PurchaseProductType.Consumable);
        }

        private static PurchaseCatalogEntry Entry(string storeProductId)
        {
            return new PurchaseCatalogEntry(
                new PurchaseProductId("starter-pack"),
                PurchaseProductType.Consumable,
                storeProductId,
                storeProductId,
                storeProductId,
                true,
                1);
        }

        private static UnityValidatedReceipt Receipt(string transactionId, string productId, string applicationId, string receiptType)
        {
            return new UnityValidatedReceipt(
                PurchaseStoreNames.GooglePlay,
                productId,
                transactionId,
                applicationId,
                new DateTime(2026, 7, 24, 0, 0, 0, DateTimeKind.Utc),
                receiptType);
        }

        private sealed class FakeBackend : IUnityReceiptValidationBackend
        {
            private readonly UnityValidatedReceipt[] receipts;

            private FakeBackend(bool isConfigured, string configurationErrorCode, UnityValidatedReceipt[] receipts)
            {
                IsConfigured = isConfigured;
                ConfigurationErrorCode = configurationErrorCode;
                this.receipts = receipts;
            }

            public bool IsConfigured { get; }
            public string ConfigurationErrorCode { get; }
            public bool ThrowSecurity { get; set; }

            public static FakeBackend Configured(params UnityValidatedReceipt[] receipts)
            {
                return new FakeBackend(true, null, receipts.Length == 0 ? new[] { Receipt("tx-1", "google-starter", "com.test.game", UnityReceiptTypes.GooglePlay) } : receipts);
            }

            public static FakeBackend Misconfigured(string code) => new FakeBackend(false, code, Array.Empty<UnityValidatedReceipt>());

            public UnityValidatedReceipt[] Validate(string unityReceipt)
            {
                if (ThrowSecurity)
                    throw new UnityReceiptSecurityException("bad signature");

                return receipts;
            }
        }
    }
}

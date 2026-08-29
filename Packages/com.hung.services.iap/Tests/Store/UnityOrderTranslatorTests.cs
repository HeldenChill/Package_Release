using NUnit.Framework;
using Hung.Base;

namespace Hung.IAP.Tests
{
    public sealed class UnityOrderTranslatorTests
    {
        [TestCase(UnityOrderKind.Pending)]
        [TestCase(UnityOrderKind.Confirmed)]
        public void TryTranslatePurchaseOrder_AcceptsSinglePaidOrder(UnityOrderKind kind)
        {
            var snapshot = UnityOrderSnapshot.SingleProduct(
                kind,
                "tx-1",
                PurchaseStoreNames.GooglePlay,
                "google-starter",
                "receipt-json",
                PurchaseProductType.Consumable);

            bool ok = UnityOrderTranslator.TryTranslatePurchase(snapshot, out StorePurchaseRecord record, out string code);

            Assert.That(ok, Is.True);
            Assert.That(code, Is.Null);
            Assert.That(record.TransactionId, Is.EqualTo("tx-1"));
            Assert.That(record.StoreName, Is.EqualTo(PurchaseStoreNames.GooglePlay));
            Assert.That(record.StoreProductId, Is.EqualTo("google-starter"));
            Assert.That(record.ProductType, Is.EqualTo(PurchaseProductType.Consumable));
        }

        [Test]
        public void TryTranslatePurchaseOrder_RejectsMissingTransactionId()
        {
            var snapshot = UnityOrderSnapshot.SingleProduct(
                UnityOrderKind.Pending,
                "",
                PurchaseStoreNames.GooglePlay,
                "google-starter",
                "receipt-json",
                PurchaseProductType.Consumable);

            bool ok = UnityOrderTranslator.TryTranslatePurchase(snapshot, out _, out string code);

            Assert.That(ok, Is.False);
            Assert.That(code, Is.EqualTo(UnityOrderTranslatorCodes.TransactionIdMissing));
        }

        [Test]
        public void TryTranslatePurchaseOrder_RejectsMultiItemOrders()
        {
            var snapshot = UnityOrderSnapshot.MultiProduct(
                UnityOrderKind.Pending,
                "tx-1",
                PurchaseStoreNames.GooglePlay,
                "receipt-json",
                new UnityPurchasedProductSnapshot("google-starter", PurchaseProductType.Consumable),
                new UnityPurchasedProductSnapshot("google-gold", PurchaseProductType.Consumable));

            bool ok = UnityOrderTranslator.TryTranslatePurchase(snapshot, out _, out string code);

            Assert.That(ok, Is.False);
            Assert.That(code, Is.EqualTo(UnityOrderTranslatorCodes.MultiItemOrderUnsupported));
        }

        [Test]
        public void TranslateRequestResult_MapsDeferred()
        {
            var snapshot = UnityOrderSnapshot.SingleProduct(
                UnityOrderKind.Deferred,
                "",
                PurchaseStoreNames.GooglePlay,
                "google-starter",
                "",
                PurchaseProductType.Consumable);

            StoreRequestResult result = UnityOrderTranslator.TranslateRequestResult(snapshot);

            Assert.That(result.Status, Is.EqualTo(StoreRequestStatus.Deferred));
        }

        [Test]
        public void TranslateRequestResult_MapsCancellation()
        {
            var snapshot = UnityOrderSnapshot.Failed(UnityPurchaseFailureReason.UserCancelled, "cancelled");

            StoreRequestResult result = UnityOrderTranslator.TranslateRequestResult(snapshot);

            Assert.That(result.Status, Is.EqualTo(StoreRequestStatus.Cancelled));
        }

        [Test]
        public void TranslateRequestResult_MapsPurchaseFailure()
        {
            var snapshot = UnityOrderSnapshot.Failed(UnityPurchaseFailureReason.ProductUnavailable, "missing");

            StoreRequestResult result = UnityOrderTranslator.TranslateRequestResult(snapshot);

            Assert.That(result.Status, Is.EqualTo(StoreRequestStatus.Failed));
            Assert.That(result.Code, Is.EqualTo("missing"));
        }

        [Test]
        public void TranslateFetchFailure_ProducesRestoreFailure()
        {
            StoreRestoreResult result = UnityOrderTranslator.TranslateFetchFailure("network");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Code, Is.EqualTo("network"));
        }
    }
}

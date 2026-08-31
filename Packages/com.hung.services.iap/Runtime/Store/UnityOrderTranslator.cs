using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine.Purchasing;

namespace Hung.IAP
{
    public enum UnityOrderKind
    {
        Pending,
        Confirmed,
        Failed,
        Deferred
    }

    public enum UnityPurchaseFailureReason
    {
        UserCancelled,
        ProductUnavailable,
        StoreNotConnected,
        Unknown
    }

    public static class UnityOrderTranslatorCodes
    {
        public const string TransactionIdMissing = "UNITY_PURCHASE_TRANSACTION_ID_MISSING";
        public const string MultiItemOrderUnsupported = "UNITY_PURCHASE_MULTI_ITEM_ORDER_UNSUPPORTED";
        public const string ProductMissing = "UNITY_PURCHASE_PRODUCT_MISSING";
    }

    public readonly struct UnityPurchasedProductSnapshot
    {
        public UnityPurchasedProductSnapshot(string storeProductId, PurchaseProductType productType)
        {
            StoreProductId = storeProductId;
            ProductType = productType;
        }

        public string StoreProductId { get; }
        public PurchaseProductType ProductType { get; }
    }

    public sealed class UnityOrderSnapshot
    {
        private UnityOrderSnapshot(
            UnityOrderKind kind,
            string transactionId,
            string storeName,
            string receipt,
            IReadOnlyList<UnityPurchasedProductSnapshot> products,
            UnityPurchaseFailureReason failureReason,
            string failureCode)
        {
            Kind = kind;
            TransactionId = transactionId;
            StoreName = storeName;
            Receipt = receipt;
            Products = products ?? Array.Empty<UnityPurchasedProductSnapshot>();
            FailureReason = failureReason;
            FailureCode = failureCode;
        }

        public UnityOrderKind Kind { get; }
        public string TransactionId { get; }
        public string StoreName { get; }
        public string Receipt { get; }
        public IReadOnlyList<UnityPurchasedProductSnapshot> Products { get; }
        public UnityPurchaseFailureReason FailureReason { get; }
        public string FailureCode { get; }

        public static UnityOrderSnapshot SingleProduct(
            UnityOrderKind kind,
            string transactionId,
            string storeName,
            string storeProductId,
            string receipt,
            PurchaseProductType productType)
        {
            return new UnityOrderSnapshot(
                kind,
                transactionId,
                storeName,
                receipt,
                new[] { new UnityPurchasedProductSnapshot(storeProductId, productType) },
                UnityPurchaseFailureReason.Unknown,
                null);
        }

        public static UnityOrderSnapshot MultiProduct(
            UnityOrderKind kind,
            string transactionId,
            string storeName,
            string receipt,
            params UnityPurchasedProductSnapshot[] products)
        {
            return new UnityOrderSnapshot(kind, transactionId, storeName, receipt, products, UnityPurchaseFailureReason.Unknown, null);
        }

        public static UnityOrderSnapshot Failed(UnityPurchaseFailureReason reason, string code)
        {
            return new UnityOrderSnapshot(UnityOrderKind.Failed, null, null, null, Array.Empty<UnityPurchasedProductSnapshot>(), reason, code);
        }
    }

    public static class UnityOrderTranslator
    {
        public static UnityOrderSnapshot FromUnityOrder(Order order, UnityOrderKind kind, string storeName)
        {
            var products = new List<UnityPurchasedProductSnapshot>();
            IReadOnlyList<CartItem> cartItems = order.CartOrdered?.Items();
            if (cartItems != null)
            {
                foreach (CartItem item in cartItems)
                {
                    if (item?.Product?.definition == null)
                        continue;

                    products.Add(new UnityPurchasedProductSnapshot(
                        item.Product.definition.storeSpecificId,
                        ConvertProductType(item.Product.definition.type)));
                }
            }

            return UnityOrderSnapshot.MultiProduct(
                kind,
                order.Info?.TransactionID,
                storeName,
                order.Info?.Receipt,
                products.ToArray());
        }

        public static UnityOrderSnapshot FromUnityFailedOrder(FailedOrder order)
        {
            return UnityOrderSnapshot.Failed(ConvertFailureReason(order.FailureReason), order.Details);
        }

        public static bool TryTranslatePurchase(UnityOrderSnapshot snapshot, out StorePurchaseRecord record, out string code)
        {
            if (string.IsNullOrEmpty(snapshot.TransactionId))
            {
                record = default;
                code = UnityOrderTranslatorCodes.TransactionIdMissing;
                return false;
            }

            if (snapshot.Products == null || snapshot.Products.Count == 0)
            {
                record = default;
                code = UnityOrderTranslatorCodes.ProductMissing;
                return false;
            }

            if (snapshot.Products.Count != 1)
            {
                record = default;
                code = UnityOrderTranslatorCodes.MultiItemOrderUnsupported;
                return false;
            }

            UnityPurchasedProductSnapshot product = snapshot.Products[0];
            record = new StorePurchaseRecord(
                snapshot.TransactionId,
                snapshot.StoreName,
                product.StoreProductId,
                snapshot.Receipt,
                Fingerprint(snapshot.Receipt),
                product.ProductType);
            code = null;
            return true;
        }

        public static StoreRequestResult TranslateRequestResult(UnityOrderSnapshot snapshot)
        {
            if (snapshot.Kind == UnityOrderKind.Deferred)
                return StoreRequestResult.Deferred(snapshot.FailureCode);
            if (snapshot.Kind == UnityOrderKind.Failed)
            {
                if (snapshot.FailureReason == UnityPurchaseFailureReason.UserCancelled)
                    return StoreRequestResult.Cancelled(snapshot.FailureCode);

                return StoreRequestResult.Failed(snapshot.FailureCode);
            }

            return TryTranslatePurchase(snapshot, out StorePurchaseRecord record, out string code)
                ? StoreRequestResult.Observed(record)
                : StoreRequestResult.Failed(code);
        }

        public static StoreRestoreResult TranslateFetchFailure(string code)
        {
            return StoreRestoreResult.Failed(code);
        }

        private static string Fingerprint(string receipt)
        {
            if (string.IsNullOrEmpty(receipt))
                return string.Empty;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < receipt.Length; i++)
                    hash = hash * 31 + receipt[i];

                return hash.ToString("x8");
            }
        }

        private static PurchaseProductType ConvertProductType(UnityEngine.Purchasing.ProductType type)
        {
            switch (type)
            {
                case UnityEngine.Purchasing.ProductType.Consumable:
                    return PurchaseProductType.Consumable;
                case UnityEngine.Purchasing.ProductType.NonConsumable:
                    return PurchaseProductType.PermanentEntitlement;
                case UnityEngine.Purchasing.ProductType.Subscription:
                    return PurchaseProductType.Subscription;
                default:
                    return PurchaseProductType.Consumable;
            }
        }

        private static UnityPurchaseFailureReason ConvertFailureReason(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.UserCancelled:
                    return UnityPurchaseFailureReason.UserCancelled;
                case PurchaseFailureReason.ProductUnavailable:
                    return UnityPurchaseFailureReason.ProductUnavailable;
                case PurchaseFailureReason.StoreNotConnected:
                    return UnityPurchaseFailureReason.StoreNotConnected;
                default:
                    return UnityPurchaseFailureReason.Unknown;
            }
        }
    }
}

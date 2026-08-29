using System;
using System.Collections.Generic;
using Hung.Base;

namespace Hung.IAP
{
    public static class PurchaseStoreNames
    {
        public const string GooglePlay = "GooglePlay";
        public const string AppleAppStore = "AppleAppStore";
        public const string Editor = "Editor";
    }

    public sealed class PurchaseCatalogEntry
    {
        public PurchaseCatalogEntry(
            PurchaseProductId productId,
            PurchaseProductType type,
            string googlePlayId,
            string appleAppStoreId,
            string editorTestId,
            bool enabled,
            int catalogVersion)
        {
            ProductId = productId;
            Type = type;
            GooglePlayId = googlePlayId;
            AppleAppStoreId = appleAppStoreId;
            EditorTestId = editorTestId;
            Enabled = enabled;
            CatalogVersion = catalogVersion;
        }

        public PurchaseProductId ProductId { get; }

        public PurchaseProductType Type { get; }

        public string GooglePlayId { get; }

        public string AppleAppStoreId { get; }

        public string EditorTestId { get; }

        public bool Enabled { get; }

        public int CatalogVersion { get; }
    }

    public sealed class PurchaseCatalog : IPurchaseCatalogProvider
    {
        private readonly Dictionary<PurchaseProductId, PurchaseCatalogEntry> entriesById = new();
        private readonly Dictionary<string, PurchaseCatalogEntry> entriesByStoreId = new(StringComparer.Ordinal);

        public PurchaseCatalog(IEnumerable<PurchaseCatalogEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            int version = 0;
            foreach (PurchaseCatalogEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Catalog entries cannot contain null.", nameof(entries));

                if (!entry.ProductId.IsValid)
                    throw new ArgumentException("Catalog entry has invalid logical product id.", nameof(entries));

                if (entry.Type == PurchaseProductType.Subscription)
                    throw new NotSupportedException("Subscriptions are not supported in this purchase integrity wave.");

                if (entriesById.ContainsKey(entry.ProductId))
                    throw new InvalidOperationException($"Duplicate purchase product id '{entry.ProductId}'.");

                entriesById.Add(entry.ProductId, entry);
                version = Math.Max(version, entry.CatalogVersion);

                RegisterStoreId(PurchaseStoreNames.GooglePlay, entry.GooglePlayId, entry);
                RegisterStoreId(PurchaseStoreNames.AppleAppStore, entry.AppleAppStoreId, entry);
                RegisterStoreId(PurchaseStoreNames.Editor, entry.EditorTestId, entry);
            }

            Version = version;
        }

        public int Version { get; }

        public bool TryGet(PurchaseProductId id, out PurchaseCatalogEntry entry)
        {
            if (entriesById.TryGetValue(id, out entry) && entry.Enabled)
                return true;

            entry = null;
            return false;
        }

        public bool TryResolveStoreId(string storeName, string storeProductId, out PurchaseCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(storeName) || string.IsNullOrEmpty(storeProductId))
            {
                entry = null;
                return false;
            }

            if (entriesByStoreId.TryGetValue(MakeStoreKey(storeName, storeProductId), out entry) && entry.Enabled)
                return true;

            entry = null;
            return false;
        }

        public static PurchaseCatalogValidationReport ValidateReleasedProducts(
            IEnumerable<PurchaseCatalogEntry> released,
            IEnumerable<PurchaseCatalogEntry> candidate)
        {
            var report = new PurchaseCatalogValidationReport();
            var releasedById = new Dictionary<PurchaseProductId, PurchaseCatalogEntry>();

            if (released != null)
            {
                foreach (PurchaseCatalogEntry entry in released)
                {
                    if (entry != null && entry.ProductId.IsValid && !releasedById.ContainsKey(entry.ProductId))
                        releasedById.Add(entry.ProductId, entry);
                }
            }

            if (candidate == null)
                return report;

            foreach (PurchaseCatalogEntry entry in candidate)
            {
                if (entry == null || !releasedById.TryGetValue(entry.ProductId, out PurchaseCatalogEntry old))
                    continue;

                if (entry.Type != old.Type)
                    report.AddError(entry.ProductId, "PRODUCT_TYPE_CHANGED");
                if (!string.Equals(entry.GooglePlayId, old.GooglePlayId, StringComparison.Ordinal))
                    report.AddError(entry.ProductId, "GOOGLE_PLAY_ID_CHANGED");
                if (!string.Equals(entry.AppleAppStoreId, old.AppleAppStoreId, StringComparison.Ordinal))
                    report.AddError(entry.ProductId, "APPLE_APP_STORE_ID_CHANGED");
                if (!string.Equals(entry.EditorTestId, old.EditorTestId, StringComparison.Ordinal))
                    report.AddError(entry.ProductId, "EDITOR_TEST_ID_CHANGED");
            }

            return report;
        }

        private void RegisterStoreId(string storeName, string storeProductId, PurchaseCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(storeProductId))
                return;

            string key = MakeStoreKey(storeName, storeProductId);
            if (entriesByStoreId.ContainsKey(key))
                throw new InvalidOperationException($"Duplicate store product id '{storeProductId}' for store '{storeName}'.");

            entriesByStoreId.Add(key, entry);
        }

        private static string MakeStoreKey(string storeName, string storeProductId)
        {
            return storeName + "\n" + storeProductId;
        }
    }

    public sealed class PurchaseCatalogValidationReport
    {
        private readonly List<PurchaseCatalogValidationError> errors = new();

        public IReadOnlyList<PurchaseCatalogValidationError> Errors => errors.AsReadOnly();

        public bool HasErrors => errors.Count > 0;

        internal void AddError(PurchaseProductId productId, string code)
        {
            errors.Add(new PurchaseCatalogValidationError(productId, code));
        }
    }

    public readonly struct PurchaseCatalogValidationError
    {
        public PurchaseCatalogValidationError(PurchaseProductId productId, string code)
        {
            ProductId = productId;
            Code = code;
        }

        public PurchaseProductId ProductId { get; }

        public string Code { get; }
    }
}

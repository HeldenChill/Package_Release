using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.IAP
{
    [CreateAssetMenu(fileName = "Purchase Catalog", menuName = "Hung/Purchasing/Purchase Catalog")]
    public sealed class PurchaseCatalogSO : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private List<Entry> entries = new();

        public int Version => version;

        public PurchaseCatalog Build()
        {
            var runtimeEntries = new List<PurchaseCatalogEntry>(entries.Count);
            foreach (Entry entry in entries)
                runtimeEntries.Add(entry.ToRuntime(version));

            return new PurchaseCatalog(runtimeEntries);
        }

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string productId;
            [SerializeField] private PurchaseProductType type;
            [SerializeField] private string googlePlayId;
            [SerializeField] private string appleAppStoreId;
            [SerializeField] private string editorTestId;
            [SerializeField] private bool enabled = true;

            public PurchaseCatalogEntry ToRuntime(int catalogVersion)
            {
                return new PurchaseCatalogEntry(
                    new PurchaseProductId(productId),
                    type,
                    googlePlayId,
                    appleAppStoreId,
                    editorTestId,
                    enabled,
                    catalogVersion);
            }
        }
    }
}

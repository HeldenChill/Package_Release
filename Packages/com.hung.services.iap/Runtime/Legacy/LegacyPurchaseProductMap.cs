using System;
using System.Collections.Generic;
using Hung.Base;

namespace Hung.IAP
{
    public sealed class LegacyPurchaseProductMap
    {
        private readonly Dictionary<IAP_ITEM, PurchaseProductId> productIdsByLegacyItem = new();

        public LegacyPurchaseProductMap(IEnumerable<Entry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            foreach (Entry entry in entries)
            {
                if (productIdsByLegacyItem.TryGetValue(entry.Item, out PurchaseProductId existing))
                {
                    if (existing != entry.ProductId)
                        throw new InvalidOperationException($"Conflicting mapping for legacy IAP item '{entry.Item}'.");

                    continue;
                }

                productIdsByLegacyItem.Add(entry.Item, entry.ProductId);
            }
        }

        public bool TryGet(IAP_ITEM item, out PurchaseProductId productId)
        {
            return productIdsByLegacyItem.TryGetValue(item, out productId);
        }

        public readonly struct Entry
        {
            public Entry(IAP_ITEM item, PurchaseProductId productId)
            {
                if (!productId.IsValid)
                    throw new ArgumentException("Legacy mapping requires a valid product id.", nameof(productId));

                Item = item;
                ProductId = productId;
            }

            public IAP_ITEM Item { get; }

            public PurchaseProductId ProductId { get; }
        }
    }
}

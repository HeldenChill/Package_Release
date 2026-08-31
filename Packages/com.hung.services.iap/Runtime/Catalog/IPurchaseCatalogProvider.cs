using Hung.Base;

namespace Hung.IAP
{
    public interface IPurchaseCatalogProvider
    {
        int Version { get; }

        bool TryGet(PurchaseProductId id, out PurchaseCatalogEntry entry);

        bool TryResolveStoreId(string storeName, string storeProductId, out PurchaseCatalogEntry entry);
    }
}

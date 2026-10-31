using Hung.Base;
namespace Hung.Data
{
    public sealed class ItemCatalogPresentationProvider : IItemPresentationProvider
    {
        private readonly ItemCatalog catalog;
        public ItemCatalogPresentationProvider(ItemCatalog catalog) { this.catalog = catalog; }
        public ItemPresentationStatus GetPresentation(ItemId id, out ItemPresentation presentation)
        {
            presentation = default;
            if (catalog == null) return ItemPresentationStatus.Unavailable;
            if (!id.IsValid) return ItemPresentationStatus.InvalidItem;
            if (!catalog.TryGet(id, out var row)) return ItemPresentationStatus.UnknownItem;
            presentation = new ItemPresentation(row.Icon, row.ShowIcon, row.DisplayName, row.Rarity);
            return ItemPresentationStatus.Found;
        }
    }
}

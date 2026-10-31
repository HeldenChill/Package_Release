namespace Hung.Base
{
    /// <summary>Newtonsoft JSON converter for <see cref="ItemId"/>.</summary>
    public sealed class ItemIdJsonConverter : StableIdJsonConverter<ItemId>
    {
        /// <inheritdoc />
        protected override bool TryParse(string raw, out ItemId id) => ItemId.TryParse(raw, out id);
    }

    /// <summary>Type converter for <see cref="ItemId"/>.</summary>
    public sealed class ItemIdTypeConverter : StableIdTypeConverter<ItemId>
    {
        /// <inheritdoc />
        protected override ItemId Parse(string raw) => ItemId.Parse(raw);
    }
}

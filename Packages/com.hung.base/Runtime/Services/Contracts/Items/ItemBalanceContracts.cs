namespace Hung.Base
{
    public enum ItemBalanceStatus
    {
        Unavailable = 0,
        Found,
        UnknownItem,
        InvalidItem
    }

    /// <summary>Detached snapshot. The product defines lock semantics.</summary>
    public readonly struct ItemBalance
    {
        public ItemBalance(ItemId itemId, int quantity, int lockQuantity)
        {
            ItemId = itemId;
            Quantity = quantity;
            LockQuantity = lockQuantity;
        }

        public ItemId ItemId { get; }
        public int Quantity { get; }
        public int LockQuantity { get; }
    }

    public interface IItemBalanceReader
    {
        // Found includes zero. Every non-Found outcome returns default balance.
        ItemBalanceStatus GetBalance(ItemId itemId, out ItemBalance balance);
    }
}

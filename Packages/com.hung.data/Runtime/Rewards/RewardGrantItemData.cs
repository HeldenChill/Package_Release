using System;

namespace Hung.Data.Rewards
{
    [Serializable]
    public sealed class RewardGrantItemData
    {
        public string itemId;
        public int quantity;

        public RewardGrantItemData()
        {
        }

        public RewardGrantItemData(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }
}

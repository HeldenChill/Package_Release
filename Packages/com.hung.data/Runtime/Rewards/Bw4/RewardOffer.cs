using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;

namespace Hung.Data.Rewards
{
    [Serializable] public sealed class RewardLine
    {
        public string itemId;
        public int quantity;
        public RewardLine() { }
        public RewardLine(string id, int amount) { itemId = id; quantity = amount; }
    }
    [Serializable] public sealed class RewardOfferDefinition
    {
        public string key;
        public string revision;
        public List<RewardLine> items = new List<RewardLine>();
        public ResolvedRewardOffer Resolve(Func<ItemId, bool> accepts)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(revision) || accepts == null)
                throw new ArgumentException("Reward definition requires key, revision and inventory policy.");
            var lines = RewardPayload.Canonical(items == null ? null : items.Select(x =>
            {
                if (x == null) throw new ArgumentException("Null reward line.");
                return new RewardGrantItem(ItemId.Parse(x.itemId), x.quantity);
            }).ToArray());
            if (lines.Any(x => !accepts(x.ItemId))) throw new ArgumentException("Unknown inventory item.");
            return new ResolvedRewardOffer(key, revision, lines);
        }
    }
    public sealed class ResolvedRewardOffer
    {
        public ResolvedRewardOffer(string key, string revision, IReadOnlyList<RewardGrantItem> items)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Missing offer identity.");
            Key = key; Revision = revision; Items = RewardPayload.Canonical(items);
        }
        public string Key { get; }
        public string Revision { get; }
        public IReadOnlyList<RewardGrantItem> Items { get; }
    }
    public static class RewardPayload
    {
        public static IReadOnlyList<RewardGrantItem> Canonical(IReadOnlyList<RewardGrantItem> items)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("Empty reward.");
            var sums = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (!item.ItemId.IsValid || item.Quantity <= 0) throw new ArgumentException("Invalid reward.");
                sums.TryGetValue(item.ItemId.Value, out int old);
                sums[item.ItemId.Value] = checked(old + item.Quantity);
            }
            return Array.AsReadOnly(sums.Select(x => new RewardGrantItem(ItemId.Parse(x.Key), x.Value)).ToArray());
        }
    }
}

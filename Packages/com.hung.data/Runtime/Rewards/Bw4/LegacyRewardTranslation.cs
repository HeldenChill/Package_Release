using System;
using System.Collections.Generic;
using Hung.Base;
namespace Hung.Data.Rewards
{
    // Host supplies every translation, including direct items. No built-in bundle semantics.
    public sealed class LegacyRewardTranslation
    {
        private readonly Dictionary<string, ResolvedRewardOffer> entries = new Dictionary<string, ResolvedRewardOffer>(StringComparer.Ordinal);
        private static string Key(ItemId id, int quantity) => id.Value + ":" + quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public void Add(ItemId oldId, int oldQuantity, ResolvedRewardOffer offer)
        {
            if (!oldId.IsValid || oldQuantity <= 0 || offer == null) throw new ArgumentException("Invalid translation.");
            entries.Add(Key(oldId, oldQuantity), offer);
        }
        public ResolvedRewardOffer Resolve(ItemId oldId, int oldQuantity)
        {
            if (!entries.TryGetValue(Key(oldId, oldQuantity), out var offer)) throw new ArgumentException("LEGACY_REWARD_UNMAPPED");
            return offer;
        }
    }
}

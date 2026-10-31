using Hung.Base;
namespace Hung.Data.Rewards
{
    // Host must retrieve this from a retained historical configuration, never today's wheel.
    public sealed class LegacySpinSnapshot
    {
        public LegacySpinSnapshot(ItemId legacyItem, int legacyQuantity, int index, bool isAds, string authorization, ResolvedRewardOffer offer)
        { LegacyItem = legacyItem; LegacyQuantity = legacyQuantity; Index = index; IsAds = isAds; Authorization = authorization; Offer = offer; }
        public ItemId LegacyItem { get; }
        public int LegacyQuantity { get; }
        public int Index { get; }
        public bool IsAds { get; }
        public string Authorization { get; }
        public ResolvedRewardOffer Offer { get; }
    }
}

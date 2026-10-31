using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
namespace Hung.Data.Rewards
{
    [Serializable] public sealed class PendingRewardData
    {
        public int format = 1;
        public string profile;
        public string feature;
        public string claimId;
        public string fingerprint;
        public string offerKey;
        public string revision;
        public List<RewardGrantItemData> items = new List<RewardGrantItemData>();
        public int day;
        public string mode;
        public int index;
        public int targetCount;
        public int ordinal;
        public long preparedUtcTicks;
        public string authorization;
        public static PendingRewardData Create(string profile, string feature, RewardClaimId id,
            string occurrence, ResolvedRewardOffer offer, int day, string mode, int index, int targetCount, long ticks, string authorization, int ordinal = 0)
        {
            return new PendingRewardData { profile = profile, feature = feature, claimId = id.Value,
                fingerprint = RewardFingerprintV1.Create(profile, feature, occurrence, offer),
                offerKey = offer.Key, revision = offer.Revision,
                items = offer.Items.Select(x => new RewardGrantItemData(x.ItemId.Value, x.Quantity)).ToList(),
                day = day, mode = mode, index = index, targetCount = targetCount, preparedUtcTicks = ticks,
                authorization = authorization, ordinal = ordinal };
        }
        public ResolvedRewardOffer Offer() => new ResolvedRewardOffer(offerKey, revision,
            items.Select(x => new RewardGrantItem(ItemId.Parse(x.itemId), x.quantity)).ToArray());
        public RewardClaimRequest Request() => new RewardClaimRequest(new RewardClaimId(claimId), feature,
            items.Select(x => new RewardGrantItem(ItemId.Parse(x.itemId), x.quantity)).ToArray(), fingerprint);
        public bool Valid()
        {
            try
            {
                return format == 1 && !string.IsNullOrWhiteSpace(profile) && !string.IsNullOrWhiteSpace(feature)
                    && !string.IsNullOrWhiteSpace(claimId) && !string.IsNullOrWhiteSpace(fingerprint)
                    && !string.IsNullOrWhiteSpace(mode) && !string.IsNullOrWhiteSpace(authorization)
                    && day > 0 && index >= 0 && targetCount > 0 && preparedUtcTicks > 0
                    && preparedUtcTicks <= DateTime.MaxValue.Ticks && (feature != "spin-wheel" || ordinal > 0) && Offer().Items.Count > 0;
            }
            catch (Exception) { return false; }
        }
    }
}

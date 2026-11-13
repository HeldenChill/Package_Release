using System;
using System.Collections.Generic;

namespace Hung.Data.Rewards
{
    [Serializable]
    public sealed class RewardIntegrityStateData
    {
        public long latestObservedUtcTicks;
        public int resetOffsetMinutes;
        public List<RewardClaimRecordData> claims = new();

        public static RewardIntegrityStateData CreateDefault() => new RewardIntegrityStateData();
    }
}

using System.Collections.Generic;
using Hung.Base;
using Hung.Data.LiveOps;
using UnityEngine;

namespace Hung.LiveOps.DailyGift.Tests
{
    public sealed class DailyGiftTestConfig : ScriptableObject, IDailyGiftConfig
    {
        public readonly List<DailyGiftTestDay> daily = new();
        public readonly List<DailyGiftTestDay> streak = new();

        public int LevelUnlock => 3;
        public IReadOnlyList<IDailyGiftDay> DailyGifts => daily;
        public IReadOnlyList<IDailyGiftDay> StreakLoginGifts => streak;
    }

    public sealed class DailyGiftTestDay : IDailyGiftDay
    {
        public int day;
        public readonly List<DailyGiftTestReward> rewards = new();

        public int Day => day;
        public IReadOnlyList<IDailyGiftReward> Rewards => rewards;
    }

    public sealed class DailyGiftTestReward : IDailyGiftReward
    {
        public ItemId itemId;
        public int quantity;

        public ItemId ItemId => itemId;
        public int Quantity => quantity;
    }
}

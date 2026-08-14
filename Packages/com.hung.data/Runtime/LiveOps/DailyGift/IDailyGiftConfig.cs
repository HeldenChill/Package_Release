using System.Collections.Generic;
using Hung.Base;

namespace Hung.Data.LiveOps
{
    /// <summary>
    /// Defines a game-owned DailyGift configuration without prescribing its serialized model.
    /// </summary>
    public interface IDailyGiftConfig
    {
        /// <summary>Gets player level required to unlock DailyGift.</summary>
        int LevelUnlock { get; }

        /// <summary>Gets cyclic DailyGift day definitions.</summary>
        IReadOnlyList<IDailyGiftDay> DailyGifts { get; }

        /// <summary>Gets consecutive-login DailyGift day definitions.</summary>
        IReadOnlyList<IDailyGiftDay> StreakLoginGifts { get; }
    }

    /// <summary>
    /// Defines one DailyGift day and its rewards.
    /// </summary>
    public interface IDailyGiftDay
    {
        /// <summary>Gets zero-based DailyGift day index.</summary>
        int Day { get; }

        /// <summary>Gets rewards available for this day.</summary>
        IReadOnlyList<IDailyGiftReward> Rewards { get; }
    }

    /// <summary>
    /// Defines one DailyGift reward in the package-neutral item format.
    /// </summary>
    public interface IDailyGiftReward
    {
        /// <summary>Gets stable catalog item identifier.</summary>
        ItemId ItemId { get; }

        /// <summary>Gets reward quantity.</summary>
        int Quantity { get; }
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves DailyGift through the framework registry.</summary>
        public static IDailyGiftService DailyGift => ServiceSlots.Get<IDailyGiftService>();
    }
}

public enum DailyGiftTrack
{
    Normal,
    Streak
}

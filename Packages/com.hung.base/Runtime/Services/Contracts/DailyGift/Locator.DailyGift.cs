namespace Hung.Base
{
    public static partial class Locator
    {
        private static IDailyGiftService dailyGift;
        public static IDailyGiftService DailyGift
        {
            get => dailyGift;
            set => dailyGift = value;
        }
    }
}

public enum DailyGiftTrack
{
    Normal,
    Streak
}

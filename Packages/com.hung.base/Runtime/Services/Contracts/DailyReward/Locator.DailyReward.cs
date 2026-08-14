namespace Hung.Base
{
    public static partial class Locator
    {
        private static IDailyRewardService dailyReward;
        public static IDailyRewardService DailyReward
        {
            get => dailyReward;
            set => dailyReward = value;
        }
    }
}

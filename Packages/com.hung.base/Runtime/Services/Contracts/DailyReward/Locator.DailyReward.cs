namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves DailyReward through the framework registry.</summary>
        public static IDailyRewardService DailyReward => ServiceSlots.Get<IDailyRewardService>();
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Analytics through the framework registry.</summary>
        public static IAnalyticsService Analytics => ServiceSlots.Get<IAnalyticsService>();
    }
}

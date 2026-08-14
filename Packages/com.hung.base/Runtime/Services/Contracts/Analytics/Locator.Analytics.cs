namespace Hung.Base
{
    public static partial class Locator
    {
        private static IAnalyticsService analytics;
        public static IAnalyticsService Analytics
        {
            get => analytics;
            set => analytics = value;
        }
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        private static IRevenueEventSink revenueSink;
        public static IRevenueEventSink RevenueSink
        {
            get => revenueSink;
            set => revenueSink = value;
        }
    }
}

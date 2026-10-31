namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves RevenueSink through the framework registry.</summary>
        public static IRevenueEventSink RevenueSink => ServiceSlots.Get<IRevenueEventSink>();
    }
}

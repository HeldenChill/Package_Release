namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Ads through the framework registry.</summary>
        public static IAdsService Ads => ServiceSlots.Get<IAdsService>();
    }
}

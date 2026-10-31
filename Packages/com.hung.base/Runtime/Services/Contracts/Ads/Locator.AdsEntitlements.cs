namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves AdsEntitlements through the framework registry.</summary>
        public static IAdsEntitlements AdsEntitlements => ServiceSlots.Get<IAdsEntitlements>();
    }
}

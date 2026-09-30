namespace Hung.Ads
{
    using Hung.Base;

    internal static class AdsEntitlementsAccess
    {
        // Read live every check: a cached null froze the old GameData lookup forever (BUG-0337).
        public static IAdsEntitlements Current => Locator.AdsEntitlements ?? NullAdsEntitlements.Instance;
    }
}

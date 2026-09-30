namespace Hung.Base
{
    public static partial class Locator
    {
        private static IAdsEntitlements adsEntitlements;
        public static IAdsEntitlements AdsEntitlements
        {
            get => adsEntitlements;
            set => adsEntitlements = value;
        }
    }
}

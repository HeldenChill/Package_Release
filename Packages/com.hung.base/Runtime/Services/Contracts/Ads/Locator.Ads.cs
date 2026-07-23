namespace Hung.Base
{
    public static partial class Locator
    {
        private static IAdsService ads;
        public static IAdsService Ads
        {
            get => ads;
            set => ads = value;
        }
    }
}

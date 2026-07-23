namespace Hung.Base
{
    public static partial class Locator
    {
        private static global::IHeartService heart;
        public static global::IHeartService Heart
        {
            get => heart;
            set => heart = value;
        }
    }
}

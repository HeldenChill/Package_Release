namespace Hung.Base
{
    public static partial class Locator
    {
        private static IIAPService iAPService;
        public static IIAPService IAPService
        {
            get => iAPService;
            set => iAPService = value;
        }
    }
}

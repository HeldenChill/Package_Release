namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves IAPService through the framework registry.</summary>
        public static IIAPService IAPService => ServiceSlots.Get<IIAPService>();
    }
}

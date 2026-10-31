namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves UI through the framework registry.</summary>
        public static IUIService UI => ServiceSlots.Get<IUIService>();
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Items through the framework registry.</summary>
        public static IItemService Items => ServiceSlots.Get<IItemService>();
    }
}

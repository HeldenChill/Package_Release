namespace Hung.Base
{
    public static partial class Locator
    {
        private static IItemService items;
        public static IItemService Items
        {
            get => items;
            set => items = value;
        }
    }
}

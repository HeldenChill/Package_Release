namespace Hung.Base
{
    public static partial class Locator
    {
        private static IUIService ui;
        public static IUIService UI
        {
            get => ui;
            set => ui = value;
        }
    }
}

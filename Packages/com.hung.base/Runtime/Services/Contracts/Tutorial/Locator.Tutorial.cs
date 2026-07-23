namespace Hung.Base
{
    public static partial class Locator
    {
        private static ITutorialService tutorial;
        public static ITutorialService Tutorial
        {
            get => tutorial;
            set => tutorial = value;
        }
    }
}

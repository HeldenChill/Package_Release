namespace Hung.Base
{
    public static partial class Locator
    {
        private static ILevelService level;
        public static ILevelService Level
        {
            get => level;
            set => level = value;
        }

        private static IGameplayService gameplay;
        public static IGameplayService Gameplay
        {
            get => gameplay;
            set => gameplay = value;
        }
    }
}

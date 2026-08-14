namespace Hung.Base
{
    public static partial class Locator
    {
        private static IAudioService audio;
        public static IAudioService Audio
        {
            get => audio;
            set => audio = value;
        }
    }
}

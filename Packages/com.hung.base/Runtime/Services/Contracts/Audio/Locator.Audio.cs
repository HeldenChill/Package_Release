namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Audio through the framework registry.</summary>
        public static IAudioService Audio => ServiceSlots.Get<IAudioService>();
    }
}

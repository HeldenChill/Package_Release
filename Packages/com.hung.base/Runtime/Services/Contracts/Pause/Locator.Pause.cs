namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Pause through the framework registry.</summary>
        public static IPauseService Pause => ServiceSlots.Get<IPauseService>();
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Tutorial through the framework registry.</summary>
        public static ITutorialService Tutorial => ServiceSlots.Get<ITutorialService>();
    }
}

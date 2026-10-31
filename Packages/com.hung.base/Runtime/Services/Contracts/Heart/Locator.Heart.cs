namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Heart through the framework registry.</summary>
        public static global::IHeartService Heart => ServiceSlots.Get<global::IHeartService>();
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Heart through the framework registry.</summary>
        public static global::Hung.Base.IHeartService Heart => ServiceSlots.Get<global::Hung.Base.IHeartService>();
    }
}

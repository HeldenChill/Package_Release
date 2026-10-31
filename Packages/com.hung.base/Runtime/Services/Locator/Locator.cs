namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Level through the framework registry.</summary>
        public static ILevelService Level => ServiceSlots.Get<ILevelService>();

        
    }
}

namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves Input through the framework registry.</summary>
        public static IInputService Input => ServiceSlots.Get<IInputService>();
    }
}

using Hung.Base;

namespace Hung.UI
{
    /// <summary>
    /// Resolves the UI service through the framework registry.
    /// </summary>
    public static class UILocator
    {
        public static IUIService Service => ServiceSlots.Get<IUIService>();
    }
}

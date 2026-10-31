namespace Hung.Base
{
    public static partial class Locator
    {
        /// <summary>Resolves SpinWheel through the framework registry.</summary>
        public static ISpinWheelService SpinWheel => ServiceSlots.Get<ISpinWheelService>();
    }
}

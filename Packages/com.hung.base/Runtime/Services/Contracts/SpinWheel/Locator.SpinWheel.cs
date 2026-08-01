namespace Hung.Base
{
    public static partial class Locator
    {
        private static ISpinWheelService spinWheel;
        public static ISpinWheelService SpinWheel
        {
            get => spinWheel;
            set => spinWheel = value;
        }
    }
}

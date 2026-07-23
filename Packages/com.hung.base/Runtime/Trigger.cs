using Hung.Utilities.Timer;

namespace Hung.Base
{
    public class Trigger
    {
        protected readonly int resetFrame;
        protected bool value;

        public Trigger(int frameReset = 1)
        {
            resetFrame = frameReset;
        }

        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                if (value)
                {
                    TimerManager.Ins.WaitForFrame(resetFrame, ResetValue);
                }
            }
        }

        public bool GetValue() => value;

        protected void ResetValue()
        {
            value = false;
        }
    }
}

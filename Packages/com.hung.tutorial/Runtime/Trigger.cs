using Hung.Utilities.Timer;

public class Trigger
{
    protected int resetFrame;
    protected bool value = false;
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
    public Trigger(int frameReset = 1)
    {
        this.resetFrame = frameReset;
    }

    protected void ResetValue()
    {
        value = false;
    }
}

using NUnit.Framework;
using Hung.Utilities.Timer;

namespace Hung.Utilities.Tests
{
    // Characterization tests against real STimer (Packages/com.hung.utilities/Runtime/STimer/STimer.cs).
    // No manual-tick API exists — STimer only advances via TimerManager's real Update/FixedUpdate/LateUpdate
    // events (real Time.deltaTime), so EditMode coverage is limited to the time<=0 synchronous-fire path
    // and the Start/Stop state machine. Loop (isLoop=true) auto-refire and normal-duration countdown are
    // untestable without a real per-frame Update loop -> left for a PlayMode harness (B3), not descoped silently.
    public class STimerTests
    {
        [Test]
        public void Timer_FiresImmediately_WhenTimeZero()
        {
            var timer = TimerManager.Ins.PopSTimer();
            bool fired = false;

            timer.Start(0f, () => fired = true);

            Assert.IsTrue(fired);
            Assert.IsFalse(timer.IsStart);

            TimerManager.Ins.PushSTimer(timer);
        }

        [Test]
        public void Timer_Cancel_NoFire()
        {
            var timer = TimerManager.Ins.PopSTimer();
            bool fired = false;

            timer.Start(5f, () => fired = true);
            timer.Stop();

            Assert.IsFalse(fired);
            Assert.IsFalse(timer.IsStart);

            TimerManager.Ins.PushSTimer(timer);
        }

        [Test]
        public void Timer_Start_SetsRemainingTime()
        {
            var timer = TimerManager.Ins.PopSTimer();

            timer.Start(5f, () => { });

            Assert.IsTrue(timer.IsStart);
            Assert.AreEqual(5f, timer.RemainingTime);

            timer.Stop();
            TimerManager.Ins.PushSTimer(timer);
        }
    }
}

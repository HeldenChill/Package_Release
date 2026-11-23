using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Hung.Utilities.Timer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Utilities.Tests
{
    // Flow audit A6 regression tests (2026-11-14). Private Unity callbacks are driven by reflection.
    public class A6TimerAndDispatcherTests
    {
        private static void Call(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        // BUG-261114-e85d: a lone unscaled WaitForTime must start an unscaled timer.
        [Test]
        public void WaitForTime_SingleUnscaledEntry_StartsUnscaledTimer()
        {
            TimerManager manager = TimerManager.Ins;
            var inUse = Field<List<STimer>>(manager, "inUseSTimers");
            int before = inUse.Count;

            manager.WaitForTime(5f, () => { }, isUnscaleTime: true);
            Call(manager, "LateUpdate");

            Assert.AreEqual(before + 1, inUse.Count);
            STimer started = inUse[inUse.Count - 1];
            Assert.IsTrue(started.IsUnscaleTime, "unscaled request must not run on scaled time");
            manager.StopTimer(ref started);
        }

        // BUG-261114-778d: a pooled timer must not keep its previous owner's frame events or counters.
        [Test]
        public void PushSTimer_ClearsFrameEvents_AndStopClearsFixedFrameCounter()
        {
            TimerManager manager = TimerManager.Ins;
            STimer timer = manager.PopSTimer();
            int leaked = 0;
            timer.FrameUpdate += () => leaked++;
            timer.Start(3, STimer.EVENT_TYPE.FRAME_FIXED_UPDATE);
            timer.Stop();
            Assert.AreEqual(0, Field<int>(timer, "timeFixedFrame"), "Stop leaves fixed-frame countdown");
            manager.PushSTimer(timer);

            leaked = 0;
            timer.Start(1, STimer.EVENT_TYPE.FRAME_UPDATE);
            Assert.AreEqual(0, leaked, "previous owner FrameUpdate still attached");
            timer.Stop();
        }

        // BUG-261114-6a03: work enqueued by an action runs next frame, not in the same drain.
        [Test]
        public void MainThreadDispatcher_ActionEnqueuedDuringDrain_RunsNextUpdate()
        {
            var go = new GameObject("MainThreadDispatcherTest");
            try
            {
                var dispatcher = go.AddComponent<MainThreadDispatcher>();
                Call(dispatcher, "Update");
                int runs = 0;
                Action chain = null;
                chain = () => { runs++; if (runs < 3) dispatcher.Enqueue(chain); };
                dispatcher.Enqueue(chain);

                Call(dispatcher, "Update");
                Assert.AreEqual(1, runs, "re-enqueued work drained in the same Update");
                Call(dispatcher, "Update");
                Call(dispatcher, "Update");
                Assert.AreEqual(3, runs);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        // BUG-261114-6a03: a throwing action does not stop later actions.
        [Test]
        public void MainThreadDispatcher_ThrowingAction_DoesNotBlockLaterActions()
        {
            var go = new GameObject("MainThreadDispatcherThrowTest");
            try
            {
                var dispatcher = go.AddComponent<MainThreadDispatcher>();
                Call(dispatcher, "Update");
                bool later = false;
                dispatcher.Enqueue(() => throw new InvalidOperationException("a6 boom"));
                dispatcher.Enqueue(() => later = true);

                LogAssert.Expect(LogType.Exception, new Regex("a6 boom"));
                Assert.DoesNotThrow(() => Call(dispatcher, "Update"));
                Assert.IsTrue(later);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}

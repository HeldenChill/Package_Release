using NUnit.Framework;
using UnityEngine;
using Gameplay.Character.Logic;

namespace Gameplay.Character.AIBridge.Tests
{
    [TestFixture]
    public class CaptureVelocityTests
    {
        [Test]
        public void ClearPendingVelocity_PreventsFlush_AndSubsequentWriteFlushes()
        {
            var logicEvent = new LogicEvent();

            bool velocityFired = false;
            bool localVelocityFired = false;
            Vector3 lastVelocity = Vector3.zero;

            logicEvent._SetVelocity += v =>
            {
                velocityFired = true;
                lastVelocity = v;
            };
            logicEvent._SetLocalVelocityXZ += v => localVelocityFired = true;

            // In EditMode test, Time.inFixedTimeStep is false -> writes are buffered
            logicEvent.SetVelocity(new Vector3(1, 2, 3));
            logicEvent.SetLocalVelocityXZ(new Vector3(4, 0, 5));

            // Clear buffered writes
            logicEvent.ClearPendingVelocity();

            // Flush should not fire anything
            logicEvent.FlushPendingVelocity();
            Assert.IsFalse(velocityFired, "FlushPendingVelocity fired SetVelocity after ClearPendingVelocity");
            Assert.IsFalse(localVelocityFired, "FlushPendingVelocity fired SetLocalVelocityXZ after ClearPendingVelocity");

            // Subsequent write after clear still flushes
            logicEvent.SetVelocity(new Vector3(7, 8, 9));
            logicEvent.FlushPendingVelocity();
            Assert.IsTrue(velocityFired, "Subsequent write after ClearPendingVelocity failed to flush");
            Assert.AreEqual(new Vector3(7, 8, 9), lastVelocity);
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Hung.DesignPattern.ScopedPooling;

namespace Hung.DesignPattern.Tests
{
    public class ScopedPoolDelayTests
    {
        [Test]
        public void ReplaceCancelPauseZeroAndReuse_AreCheckoutSafe()
        {
            var host = new GameObject("host");
            var source = new GameObject("source");
            var prefab = source.AddComponent<ScopedPoolProbe>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("units", new PoolDefinition(prefab, PoolKind.Unit));
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var first);
                scope.ScheduleReturn(first, .3f, PoolClock.Unscaled, out var old);
                scope.ScheduleReturn(first, .3f, PoolClock.Unscaled, out var replacement);
                Assert.AreEqual(PoolStatus.StaleRequest, scope.Cancel(old));
                scope.Tick(0, .2f);
                Assert.IsTrue(first.IsValid);
                scope.Tick(0, .11f);
                Assert.IsFalse(first.IsValid);
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var second);
                scope.ScheduleReturn(second, 0, PoolClock.Scaled, out var zero);
                Assert.IsTrue(second.IsValid);
                Assert.IsTrue(zero.IsValid);
                scope.Tick(0, 0);
                Assert.IsFalse(second.IsValid);
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var third);
                scope.ScheduleReturn(third, 1, PoolClock.Scaled, out var scaled);
                scope.Tick(0, 2);
                Assert.IsTrue(third.IsValid);
                Assert.AreEqual(PoolStatus.InvalidDelay, scope.ScheduleReturn(third, float.NaN, PoolClock.Unscaled, out _));
                Assert.IsTrue(scaled.IsValid, "Invalid replacement must preserve valid scheduled request");
                Assert.AreEqual(PoolStatus.Success, scope.Cancel(scaled));
                scope.ScheduleReturn(third, 1, PoolClock.Unscaled, out _);
                scope.Return(third);
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var fourth);
                scope.Tick(2, 2);
                Assert.IsTrue(fourth.IsValid);
                Assert.AreEqual(PoolStatus.InvalidDelay, scope.ScheduleReturn(fourth, float.NaN, PoolClock.Scaled, out _));
                Assert.AreEqual(PoolStatus.InvalidDelay, scope.ScheduleReturn(fourth, -1, PoolClock.Scaled, out _));
                Assert.AreEqual(PoolStatus.InvalidDelay, scope.ScheduleReturn(fourth, float.PositiveInfinity, PoolClock.Scaled, out _));
                scope.Dispose();
                Assert.IsFalse(replacement.IsValid);
            }
            finally
            {
                scope.Dispose();
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(host);
            }
        }
    }
}

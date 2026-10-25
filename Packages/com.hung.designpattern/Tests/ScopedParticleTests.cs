using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Hung.DesignPattern.ScopedPooling;

namespace Hung.DesignPattern.Tests
{
    public class ScopedParticleTests
    {
        [TearDown]
        public void ResetReentryProbe() => ScopedPoolReentryProbe.Enabled = null;

        [Test]
        public void Play_OnEnableRecheckout_DoesNotTrackStalePlayback()
        {
            var host = new GameObject("host");
            var source = new GameObject("effect");
            var particle = source.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particle.main;
            main.playOnAwake = false;
            main.loop = false;
            source.AddComponent<ScopedPoolReentryProbe>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("fx", new PoolDefinition(particle, PoolKind.Particle, hardLimit: 1));
                PoolCheckout nested = default;
                ScopedPoolReentryProbe.Enabled = () =>
                {
                    ScopedPoolReentryProbe.Enabled = null;
                    scope.Collect("fx");
                    Assert.AreEqual(PoolStatus.Success,
                        scope.Play(particle, Vector3.one, Quaternion.identity, null, false, out nested));
                };

                var status = scope.Play(particle, Vector3.zero, Quaternion.identity, null, true, out var checkout);

                Assert.AreEqual(PoolStatus.StaleCheckout, status);
                Assert.IsNull(checkout.Instance);
                Assert.IsTrue(nested.IsValid);
                Assert.AreEqual(Vector3.one, nested.Instance.transform.position);
                Assert.IsNull(typeof(PoolScope).GetField("particleTracker", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scope));
            }
            finally
            {
                ScopedPoolReentryProbe.Enabled = null;
                scope.Dispose();
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DestroyedEffect_TickRetiresSlotRequestAndCompletionTracking()
        {
            var host = new GameObject("host");
            var source = new GameObject("effect");
            var particle = source.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particle.main;
            main.playOnAwake = false;
            main.loop = false;
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("fx", new PoolDefinition(particle, PoolKind.Particle));
                scope.Play(particle, Vector3.zero, Quaternion.identity, null, true, out var checkout);
                scope.ScheduleReturn(checkout, 100, PoolClock.Unscaled, out var request);
                Object.DestroyImmediate(checkout.Instance.gameObject);

                scope.Tick(0, 0);

                Assert.AreEqual(0, ((IDictionary)typeof(PoolScope).GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scope)).Count);
                Assert.IsFalse(request.IsValid);
                var tracker = typeof(PoolScope).GetField("particleTracker", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scope);
                Assert.AreEqual(0, ((ICollection)tracker.GetType().GetField("tracked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tracker)).Count);
            }
            finally
            {
                scope.Dispose();
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LoopRequiresExplicitReturn_AndAuthoredLoopStaysUnchanged()
        {
            var host = new GameObject("host");
            var source = new GameObject("effect");
            var particle = source.AddComponent<ParticleSystem>();
            var main = particle.main;
            main.playOnAwake = false;
            main.loop = true;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                Assert.AreEqual(PoolStatus.Success, scope.Register("fx", new PoolDefinition(particle, PoolKind.Particle, "fx")));
                Assert.AreEqual(PoolStatus.InvalidParticleConfiguration,
                    scope.Play(new ParticlePoolId("fx"), Vector3.zero, Quaternion.identity, null, true, out _));
                Assert.AreEqual(PoolStatus.Success,
                    scope.Play(new ParticlePoolId("fx"), Vector3.zero, Quaternion.identity, null, false, out var checkout));
                scope.Tick(10, 10);
                Assert.IsTrue(checkout.IsValid);
                scope.Return(checkout);
                Assert.IsFalse(((ParticleSystem)checkout.Instance).IsAlive(true));
                Assert.IsTrue(particle.main.loop);
            }
            finally
            {
                scope.Dispose();
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(host);
            }
        }

        [TestCase(true, ParticleSystemStopAction.None)]
        [TestCase(false, ParticleSystemStopAction.Destroy)]
        [TestCase(false, ParticleSystemStopAction.Disable)]
        public void UnsafeAuthoredSettings_AreRejectedWithoutMutation(bool playOnAwake, ParticleSystemStopAction stopAction)
        {
            var host = new GameObject("host");
            var source = new GameObject("effect");
            var particle = source.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particle.main;
            main.playOnAwake = playOnAwake;
            main.stopAction = stopAction;
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                Assert.AreEqual(PoolStatus.InvalidParticleConfiguration,
                    scope.Register("unsafe", new PoolDefinition(particle, PoolKind.Particle, "fx", 1)));
                Assert.AreEqual(playOnAwake, particle.main.playOnAwake);
                Assert.AreEqual(stopAction, particle.main.stopAction);
                Assert.AreEqual(0, host.transform.childCount);
            }
            finally
            {
                scope.Dispose();
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ChildCompletionPreventsEarlyReturn()
        {
            var host = new GameObject("host");
            var source = new GameObject("effect");
            var root = source.AddComponent<ParticleSystem>();
            var childObject = new GameObject("child");
            childObject.transform.SetParent(source.transform);
            var child = childObject.AddComponent<ParticleSystem>();
            foreach (var ps in new[] { root, child })
            {
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("fx", new PoolDefinition(root, PoolKind.Particle, "fx"));
                scope.Play(new ParticlePoolId("fx"), Vector3.zero, Quaternion.identity, null, true, out var token);
                var copy = (ParticleSystem)token.Instance;
                copy.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var copiedChild = copy.transform.GetChild(0).GetComponent<ParticleSystem>();
                copiedChild.Emit(1);
                scope.Tick(0, 0);
                Assert.IsTrue(token.IsValid);
                copiedChild.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                scope.Tick(0, 0);
                Assert.IsFalse(token.IsValid);
                Assert.AreEqual(PoolStatus.Success,
                    scope.Play(root, Vector3.zero, Quaternion.identity, null, false, out var direct));
                Assert.IsTrue(direct.IsValid, "Direct prefab playback must require no identity lookup");
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

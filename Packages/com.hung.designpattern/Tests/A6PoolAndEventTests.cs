using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Hung.DesignPattern.Tests
{
    public sealed class A6TestDispatcher : Dispatcher<A6TestDispatcher> { }

    // Flow audit A6 regression tests (2026-11-14).
    public class A6PoolAndEventTests
    {
        private struct RemovedLastHandlerEvent : IEvent { }
        private class A6Unit : GameUnit { }

        // BUG-261114-2ef8: ParticlePool works without a PoolController in the scene.
        [Test]
        public void ParticlePool_Play_WithoutPoolController_CreatesRoot()
        {
            if (Object.FindObjectOfType<PoolController>() != null) Assert.Ignore("scene has a PoolController");
            typeof(ParticlePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            var psGo = new GameObject("A6Particle", typeof(ParticleSystem));
            var ps = psGo.GetComponent<ParticleSystem>();
            try
            {
                ParticleSystem played = null;
                Assert.DoesNotThrow(() => played = ParticlePool.Play(ps, Vector3.zero));
                Assert.IsNotNull(played);
            }
            finally
            {
                ParticlePool.Release(ps);
                var rootField = typeof(ParticlePool).GetField("root", BindingFlags.NonPublic | BindingFlags.Static);
                var root = rootField.GetValue(null) as Transform;
                if (root != null) Object.DestroyImmediate(root.gameObject);
                rootField.SetValue(null, null);
                Object.DestroyImmediate(psGo);
            }
        }

        // BUG-261114-0e6f: Release destroys pooled GameObjects and never touches an unpooled prefab.
        [Test]
        public void ParticlePool_Release_DestroysPooledObjects_AndLeavesUnpooledPrefab()
        {
            var psGo = new GameObject("A6ReleaseParticle", typeof(ParticleSystem));
            var ps = psGo.GetComponent<ParticleSystem>();
            var root = new GameObject("A6ReleaseRoot");
            var other = new GameObject("A6Unpooled", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            try
            {
                ParticlePool.Preload(ps, 2, root.transform);
                Assert.AreEqual(2, root.transform.childCount);
                ParticlePool.Release(ps);
                Assert.AreEqual(0, root.transform.childCount, "pooled GameObjects left alive");

                ParticlePool.Release(other);
                Assert.IsTrue(other != null, "unpooled prefab was destroyed");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(psGo);
                if (other != null) Object.DestroyImmediate(other.gameObject);
            }
        }

        // BUG-261114-b715: clamp without collect does not throw.
        [Test]
        public void SimplePool_ClampWithoutCollect_DoesNotThrow()
        {
            var prefabGo = new GameObject("A6ClampOnly");
            var prefab = prefabGo.AddComponent<A6Unit>();
            var root = new GameObject("A6ClampOnlyRoot");
            try { Assert.DoesNotThrow(() => SimplePool.Preload(prefab, 2, root.transform, collect: false, clamp: true)); }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(prefabGo);
            }
        }

        // BUG-261114-b715: clamp of one keeps one active object and never hands out the same instance twice.
        [Test]
        public void SimplePool_ClampOne_SecondSpawnIsDistinctAndOldestDespawned()
        {
            var prefabGo = new GameObject("A6ClampOne");
            var prefab = prefabGo.AddComponent<A6Unit>();
            var root = new GameObject("A6ClampRoot");
            try
            {
                SimplePool.Preload(prefab, 1, root.transform, collect: true, clamp: true);
                A6Unit first = SimplePool.Spawn<A6Unit>(prefab);
                A6Unit second = SimplePool.Spawn<A6Unit>(prefab);

                Assert.AreNotSame(first, second, "same instance handed out twice");
                Assert.IsTrue(second.gameObject.activeSelf);
                Assert.IsFalse(first.gameObject.activeSelf, "oldest despawned by clamp");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(prefabGo);
            }
        }

        // BUG-261114-027a: removing the last typed handler keeps the no-args handlers working.
        [Test]
        public void EventBus_RemovedLastTypedHandler_StillRunsNoArgsHandler()
        {
            System.Action<RemovedLastHandlerEvent> typed = _ => { };
            int noArgs = 0;
            var binding = new EventBinding<RemovedLastHandlerEvent>(typed);
            binding.Add(() => noArgs++);
            binding.Remove(typed);
            EventBus<RemovedLastHandlerEvent>.Subscribe(binding);
            try
            {
                EventBus<RemovedLastHandlerEvent>.Raise(default);
                Assert.AreEqual(1, noArgs);
            }
            finally { EventBus<RemovedLastHandlerEvent>.Unsubscribe(binding); }
        }

        // BUG-261114-5cec: posting after the last param listener left, and ClearAll clears param listeners.
        [Test]
        public void Dispatcher_ParamPost_AfterUnregister_AndClearAll()
        {
            var go = new GameObject("A6Dispatcher");
            try
            {
                var dispatcher = go.AddComponent<A6TestDispatcher>();
                int calls = 0;
                System.Action<object> listener = _ => calls++;
                dispatcher.RegisterListenerEvent(EventID.REVIVE, listener);
                dispatcher.UnregisterListenerEvent(EventID.REVIVE, listener);
                Assert.DoesNotThrow(() => dispatcher.PostEvent(EventID.REVIVE, null));

                dispatcher.RegisterListenerEvent(EventID.LEVEL_END, listener);
                dispatcher.ClearAllListenerEvent();
                dispatcher.PostEvent(EventID.LEVEL_END, null);
                Assert.AreEqual(0, calls, "param listener survived ClearAllListenerEvent");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}

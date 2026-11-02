using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Hung.DesignPattern;

namespace Hung.DesignPattern.Tests
{
    // Characterization tests against the real SimplePool (static, GameUnit-keyed) API —
    // NOT a generic Pool<T>. Adjusted from the plan's assumed API during Ph5 execution.
    public class PoolTests
    {
        private class TestGameUnit : global::Hung.DesignPattern.GameUnit { }
        private class TestComponent : MonoBehaviour { }

        private GameObject prefabGO;
        private TestGameUnit prefab;

        [SetUp]
        public void SetUp()
        {
            prefabGO = new GameObject("PoolTestPrefab");
            prefab = prefabGO.AddComponent<TestGameUnit>();
        }

        [TearDown]
        public void TearDown()
        {
            SimplePool.CollectAll();
            if (prefabGO != null) Object.DestroyImmediate(prefabGO);
        }

        [Test]
        public void Spawn_ReturnsActiveInstance()
        {
            var instance = SimplePool.Spawn<TestGameUnit>(prefab);

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.gameObject.activeSelf);
        }

        [Test]
        public void Despawn_Deactivates_AndInstanceIsReusedOnNextSpawn()
        {
            var first = SimplePool.Spawn<TestGameUnit>(prefab);
            var firstId = first.GetInstanceID();
            first.Despawn();

            Assert.IsFalse(first.gameObject.activeSelf, "Despawn should deactivate the GameObject");

            var second = SimplePool.Spawn<TestGameUnit>(prefab);

            Assert.AreEqual(firstId, second.GetInstanceID(), "the only inactive instance should be reused rather than instantiating a new one");
        }

        [Test]
        public void Preload_DoesNotThrow_AndSubsequentSpawnsWork()
        {
            Assert.DoesNotThrow(() => SimplePool.Preload(prefab, qty: 2));

            var a = SimplePool.Spawn<TestGameUnit>(prefab);
            var b = SimplePool.Spawn<TestGameUnit>(prefab);

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotEqual(a.GetInstanceID(), b.GetInstanceID());
        }

        [Test]
        public void Preload_NullPrefab_LogsErrorButDoesNotThrow()
        {
            GameObject parentGO = new GameObject("PoolTestParent");
            try
            {
                LogAssert.Expect(LogType.Error, "PoolTestParent : IS EMPTY!!!");

                Assert.DoesNotThrow(() => SimplePool.Preload(null, parent: parentGO.transform));
            }
            finally
            {
                Object.DestroyImmediate(parentGO);
            }
        }

        [Test]
        public void MiniPool_SpawnAfterOutOfOrderDespawn_ReusesInactiveSlot()
        {
            var prefabObject = new GameObject("MiniPoolPrefab");
            var prefabComponent = prefabObject.AddComponent<TestComponent>();
            var pool = new MiniPool<TestComponent>();

            try
            {
                pool.OnInit(prefabComponent, amount: 2);
                var first = pool.Spawn();
                var second = pool.Spawn();
                pool.Despawn(first);

                var replacement = pool.Spawn();

                Assert.AreEqual(first.GetInstanceID(), replacement.GetInstanceID());
                Assert.AreNotEqual(second.GetInstanceID(), replacement.GetInstanceID());
            }
            finally
            {
                foreach (var pooled in pool.GetActiveList()) Object.DestroyImmediate(pooled.gameObject);
                Object.DestroyImmediate(prefabObject);
            }
        }

        [Test]
        public void MiniPool_Collect_DeactivatesEveryActiveObject()
        {
            var prefabObject = new GameObject("MiniPoolPrefab");
            var prefabComponent = prefabObject.AddComponent<TestComponent>();
            var pool = new MiniPool<TestComponent>();

            try
            {
                pool.OnInit(prefabComponent, amount: 3);
                pool.Spawn();
                pool.Spawn();
                var third = pool.Spawn();
                pool.Despawn(pool.GetActiveList()[0]);
                pool.Despawn(pool.GetActiveList()[1]);

                pool.Collect();

                Assert.IsFalse(third.gameObject.activeSelf);
            }
            finally
            {
                foreach (var pooled in pool.GetActiveList()) Object.DestroyImmediate(pooled.gameObject);
                Object.DestroyImmediate(prefabObject);
            }
        }

        [Test]
        public void SimplePool_ResetSession_ClearsAllFiveStatics()
        {
            SimplePool.Preload(prefab, 1);

            var poolsField = typeof(SimplePool).GetField("Pools", BindingFlags.NonPublic | BindingFlags.Static);
            var poolTypesField = typeof(SimplePool).GetField("PoolTypes", BindingFlags.NonPublic | BindingFlags.Static);
            var poolInstanceIdField = typeof(SimplePool).GetField("PoolInstanceID", BindingFlags.NonPublic | BindingFlags.Static);
            var resourcesField = typeof(SimplePool).GetField("_gameUnitResources", BindingFlags.NonPublic | BindingFlags.Static);
            var rootField = typeof(SimplePool).GetField("_root", BindingFlags.NonPublic | BindingFlags.Static);

            var pools = (System.Collections.IDictionary)poolsField.GetValue(null);
            var poolTypes = (System.Collections.IDictionary)poolTypesField.GetValue(null);
            var poolInstanceId = (System.Collections.IDictionary)poolInstanceIdField.GetValue(null);

            Assert.Greater(pools.Count, 0, "Pools should be non-empty before reset");
            Assert.Greater(poolInstanceId.Count, 0, "PoolInstanceID should be non-empty before reset");

            resourcesField.SetValue(null, new GameUnit[] { prefab });
            GameObject dummyRoot = new GameObject("DummyRoot");
            rootField.SetValue(null, dummyRoot.transform);

            try
            {
                var resetMethod = typeof(SimplePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);
                resetMethod.Invoke(null, null);

                Assert.AreEqual(0, pools.Count, "Pools should be empty after reset");
                Assert.AreEqual(0, poolTypes.Count, "PoolTypes should be empty after reset");
                Assert.AreEqual(0, poolInstanceId.Count, "PoolInstanceID should be empty after reset");
                Assert.IsNull(resourcesField.GetValue(null), "_gameUnitResources should be null after reset");
                Assert.IsNull(rootField.GetValue(null), "_root should be null after reset");
            }
            finally
            {
                if (dummyRoot != null) Object.DestroyImmediate(dummyRoot);
            }
        }

        [Test]
        public void ParticlePool_ResetSession_ClearsBothStatics()
        {
            GameObject psGo = new GameObject("TestParticleSystem");
            var ps = psGo.AddComponent<ParticleSystem>();
            GameObject dummyRoot = new GameObject("DummyParticleRoot");

            var poolsField = typeof(ParticlePool).GetField("pools", BindingFlags.NonPublic | BindingFlags.Static);
            var rootField = typeof(ParticlePool).GetField("root", BindingFlags.NonPublic | BindingFlags.Static);

            try
            {
                ParticlePool.Preload(ps, 1);

                var pools = (System.Collections.IDictionary)poolsField.GetValue(null);
                Assert.Greater(pools.Count, 0, "pools should be non-empty before reset");

                rootField.SetValue(null, dummyRoot.transform);

                var resetMethod = typeof(ParticlePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);
                resetMethod.Invoke(null, null);

                Assert.AreEqual(0, pools.Count, "pools should be empty after reset");
                Assert.IsNull(rootField.GetValue(null), "root should be null after reset");
            }
            finally
            {
                ParticlePool.Release(ps);
                if (psGo != null) Object.DestroyImmediate(psGo);
                if (dummyRoot != null) Object.DestroyImmediate(dummyRoot);
            }
        }

        [Test]
        public void Root_IsRecreated_AfterReset()
        {
            var first = SimplePool.Spawn<TestGameUnit>(prefab);
            var rootField = typeof(SimplePool).GetField("_root", BindingFlags.NonPublic | BindingFlags.Static);
            var rootTransform = (Transform)rootField.GetValue(null);
            Assert.IsNotNull(rootTransform);

            Object.DestroyImmediate(rootTransform.gameObject);

            var resetMethod = typeof(SimplePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);
            resetMethod.Invoke(null, null);

            var second = SimplePool.Spawn<TestGameUnit>(prefab);
            try
            {
                Assert.IsNotNull(second);
                Assert.IsNotNull(second.transform.parent);
            }
            finally
            {
                if (second != null && second.gameObject != null)
                {
                    if (second.transform.parent != null)
                    {
                        var parentRoot = second.transform.parent;
                        if (parentRoot.parent != null) Object.DestroyImmediate(parentRoot.parent.gameObject);
                        else Object.DestroyImmediate(parentRoot.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(second.gameObject);
                    }
                }
            }
        }

        [Test]
        public void ResetSession_DoesNotDestroyOrTouchLiveInstances()
        {
            var instance = SimplePool.Spawn<TestGameUnit>(prefab);

            try
            {
                var simpleReset = typeof(SimplePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);
                simpleReset.Invoke(null, null);

                var particleReset = typeof(ParticlePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);
                particleReset.Invoke(null, null);

                Assert.IsNotNull(instance);
                Assert.IsNotNull(instance.gameObject);
                Assert.IsTrue(instance.gameObject.activeSelf);
            }
            finally
            {
                if (instance != null && instance.gameObject != null)
                {
                    Object.DestroyImmediate(instance.gameObject);
                }
            }
        }

        [Test]
        public void ResetSession_IsIdempotent()
        {
            var simpleReset = typeof(SimplePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);
            var particleReset = typeof(ParticlePool).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.DoesNotThrow(() =>
            {
                simpleReset.Invoke(null, null);
                particleReset.Invoke(null, null);
                simpleReset.Invoke(null, null);
                particleReset.Invoke(null, null);
            });
        }
    }
}

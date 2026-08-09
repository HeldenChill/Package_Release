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
    }
}

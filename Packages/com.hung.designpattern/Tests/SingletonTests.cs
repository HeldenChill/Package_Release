using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Hung.DesignPattern.Tests
{
    // Regression tests for the Unity fake-null bug fixed in 0.4.1: `_instance` is static and
    // survives scene unload / domain-reload-disabled play sessions, so a destroyed instance must
    // be detected via Unity's overloaded `==` (not `is not null`) or `Ins` returns the dead object
    // forever. Found in Horror1Game: CameraController.Ins handed back a destroyed controller whose
    // serialized camera list threw MissingReferenceException on every access.
    public class SingletonTests
    {
        private class TestSingleton : Singleton<TestSingleton> { }

        // Singleton<T> has no reset hook; the static cache must be cleared between cases by hand.
        private static void ClearStaticInstance()
        {
            typeof(Singleton<TestSingleton>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);
        }

        [SetUp]
        public void SetUp() => ClearStaticInstance();

        [TearDown]
        public void TearDown()
        {
            foreach (TestSingleton s in Object.FindObjectsByType<TestSingleton>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(s.gameObject);
            }
            ClearStaticInstance();
        }

        [Test]
        public void Ins_FindsExistingInstanceInScene()
        {
            TestSingleton existing = new GameObject("Existing").AddComponent<TestSingleton>();

            Assert.AreSame(existing, TestSingleton.Ins);
        }

        [Test]
        public void Ins_CreatesInstanceWhenNoneExists()
        {
            Assert.IsNotNull(TestSingleton.Ins);
        }

        [Test]
        public void Ins_AfterInstanceDestroyed_ReturnsLiveInstanceNotTheDeadOne()
        {
            TestSingleton first = TestSingleton.Ins;
            Object.DestroyImmediate(first.gameObject);

            TestSingleton second = TestSingleton.Ins;

            Assert.IsTrue(second != null, "Ins returned a destroyed (fake-null) instance.");
            Assert.AreNotSame(first, second);
            Assert.IsNotNull(second.gameObject); // threw MissingReferenceException before 0.4.1
        }
    }
}

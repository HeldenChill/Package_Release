using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base.Init;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hung.Base.Tests.Init
{
    [TestFixture]
    public class InitToggleTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _createdObjects.Clear();
        }

        [Test]
        public void InitToggle_SetValue_RaisesValueChangedWithId()
        {
            var go = new GameObject("ToggleGo");
            _createdObjects.Add(go);
            var toggle = go.AddComponent<Toggle>();
            var initToggle = go.AddComponent<InitToggle>();

            var idField = typeof(InitToggle).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
            var toggleField = typeof(InitToggle).GetField("toggle", BindingFlags.Instance | BindingFlags.NonPublic);
            var startMethod = typeof(InitToggle).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            idField?.SetValue(initToggle, 7);
            toggleField?.SetValue(initToggle, toggle);
            startMethod?.Invoke(initToggle, null);

            int recordedId = -1;
            bool recordedValue = false;
            int callCount = 0;

            initToggle.ValueChanged += (id, val) =>
            {
                callCount++;
                recordedId = id;
                recordedValue = val;
            };

            initToggle.SetValue(true);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(7, recordedId);
            Assert.IsTrue(recordedValue);
        }

        [Test]
        public void InitToggle_Destroy_WithoutToggle_DoesNotThrow()
        {
            var go = new GameObject("ToggleGo");
            _createdObjects.Add(go);
            var initToggle = go.AddComponent<InitToggle>();

            var onDestroyMethod = typeof(InitToggle).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.DoesNotThrow(() => onDestroyMethod?.Invoke(initToggle, null));
        }

        [Test]
        public void InitToggle_SetValue_WithoutToggle_DoesNotThrow()
        {
            var go = new GameObject("ToggleGo");
            _createdObjects.Add(go);
            var initToggle = go.AddComponent<InitToggle>();

            Assert.DoesNotThrow(() => initToggle.SetValue(true));
        }
    }
}

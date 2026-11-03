using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base.Init;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hung.Base.Tests.Init
{
    public class ProbeDebugManager : DebugManager
    {
        public int Writes;
        public int LastLevel;

        protected override void PersistSelectedLevel(int selectedLevel)
        {
            Writes++;
            LastLevel = selectedLevel;
        }

        public void Prepare()
        {
            FpsDebug = new GameObject("fps");
            LogDebug = new GameObject("log");
        }

        public void ClearInstance()
        {
            if (_instance == this) _instance = null;
        }

        public void Cleanup()
        {
            ClearInstance();
            if (FpsDebug != null) Object.DestroyImmediate(FpsDebug);
            if (LogDebug != null) Object.DestroyImmediate(LogDebug);
            if (gameObject != null) Object.DestroyImmediate(gameObject);
        }
    }

    public class ProbeInitManager : InitManager
    {
        public int Loads;

        protected override void LoadNextScene()
        {
            Loads++;
        }
    }

    [TestFixture]
    public class InitFlowTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private readonly List<ProbeDebugManager> _probes = new List<ProbeDebugManager>();

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                field = typeof(InitManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            }
            field?.SetValue(target, value);
        }

        private static void ResetAutomation()
        {
            var method = typeof(InitManager).GetMethod("ResetAutomationFlag", BindingFlags.Static | BindingFlags.NonPublic);
            method?.Invoke(null, null);
        }

        private ProbeDebugManager CreateProbe(string name = "ProbeDebugManager")
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            var probe = go.AddComponent<ProbeDebugManager>();
            probe.Prepare();
            _probes.Add(probe);
            return probe;
        }

        [SetUp]
        public void SetUp()
        {
            ResetAutomation();
            if (DebugManager.Ins != null)
            {
                (DebugManager.Ins as ProbeDebugManager)?.ClearInstance();
            }
        }

        [TearDown]
        public void TearDown()
        {
            ResetAutomation();

            if (DebugManager.Ins != null)
            {
                var insGo = DebugManager.Ins.gameObject;
                (DebugManager.Ins as ProbeDebugManager)?.ClearInstance();
                if (insGo != null)
                {
                    Object.DestroyImmediate(insGo);
                }
            }

            foreach (var probe in _probes)
            {
                if (probe != null)
                {
                    probe.Cleanup();
                }
            }
            _probes.Clear();

            foreach (var go in _createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _createdObjects.Clear();
        }

        // --- Task 1: DebugManager tests ---

        [Test]
        public void OnInit_NegativeLevel_DoesNotPersist()
        {
            var probe = CreateProbe();
            probe.OnInit(false, false, true, false);

            Assert.AreEqual(0, probe.Writes);
            Assert.AreEqual(-1, probe.Level);
        }

        [Test]
        public void OnInit_PositiveLevel_PersistsOnce()
        {
            var probe = CreateProbe();
            probe.OnInit(true, true, false, true, 7);

            Assert.AreEqual(1, probe.Writes);
            Assert.AreEqual(7, probe.LastLevel);
            Assert.AreEqual(7, probe.Level);
            Assert.AreEqual(false, probe.IsShowAds);
            Assert.AreEqual(true, probe.IsUseIap);
            Assert.AreSame(probe, DebugManager.Ins);
        }

        [Test]
        public void OnInit_Twice_RaisesInitializedPerCall()
        {
            int callCount = 0;
            Action handler = () => callCount++;
            DebugManager.Initialized += handler;
            try
            {
                var probe1 = CreateProbe("Probe1");
                probe1.OnInit(false, false, false, false);

                var probe2 = CreateProbe("Probe2");
                probe2.OnInit(false, false, false, false);

                Assert.AreEqual(2, callCount);
                Assert.AreSame(probe2, DebugManager.Ins);
            }
            finally
            {
                DebugManager.Initialized -= handler;
            }
        }

        [Test]
        public void SetUseIap_FlipsIsUseIap()
        {
            var probe = CreateProbe();
            probe.SetUseIap(true);
            Assert.IsTrue(probe.IsUseIap);

            probe.SetUseIap(false);
            Assert.IsFalse(probe.IsUseIap);
        }

        // --- Task 2: InitManager tests ---

        [Test]
        public void Awake_WithoutCanvas_DoesNotThrow()
        {
            var go = new GameObject("ProbeInitManagerGo");
            _createdObjects.Add(go);
            Assert.DoesNotThrow(() => go.AddComponent<ProbeInitManager>());
        }

        [Test]
        public void StartGameFromAutomation_SetsFlagAndLoadsOnce()
        {
            ResetAutomation();
            Assert.IsFalse(InitManager.StartedFromAutomation);

            var go = new GameObject("ProbeInitManagerGo");
            _createdObjects.Add(go);
            var probe = go.AddComponent<ProbeInitManager>();

            probe.StartGameFromAutomation();

            Assert.IsTrue(InitManager.StartedFromAutomation);
            Assert.AreEqual(1, probe.Loads);
        }

        [Test]
        public void StartGameFromAutomation_DebugOn_NoCanvas_OmitsLevel()
        {
            var debugGo = new GameObject("DebugObjectPrefab");
            _createdObjects.Add(debugGo);
            var debugProbe = debugGo.AddComponent<ProbeDebugManager>();
            debugProbe.Prepare();
            _probes.Add(debugProbe);

            var initGo = new GameObject("ProbeInitManagerGo");
            _createdObjects.Add(initGo);
            var initProbe = initGo.AddComponent<ProbeInitManager>();
            SetField(initProbe, "isDebug", true);
            SetField(initProbe, "debugObject", debugGo);

            initProbe.StartGameFromAutomation();

            var spawnedProbe = DebugManager.Ins as ProbeDebugManager;
            Assert.IsNotNull(spawnedProbe);
            Assert.AreEqual(-1, spawnedProbe.Level);
            Assert.AreEqual(0, spawnedProbe.Writes);
            Assert.AreEqual(1, initProbe.Loads);
        }

        // --- Task 3: InitCanvas tests ---

        [Test]
        public void InitCanvas_IsPlainMonoBehaviour()
        {
            Assert.AreEqual(typeof(MonoBehaviour), typeof(InitCanvas).BaseType);
        }

        [Test]
        public void OnDestroy_WithUnassignedFields_DoesNotThrow()
        {
            var go = new GameObject("InitCanvasGo");
            _createdObjects.Add(go);
            var canvas = go.AddComponent<InitCanvas>();
            var method = typeof(InitCanvas).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "OnDestroy method must exist on InitCanvas");
            Assert.DoesNotThrow(() => method.Invoke(canvas, null));
        }
    }
}

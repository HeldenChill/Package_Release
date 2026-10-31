using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Hung.Base;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Analytics.Tests
{
    public sealed class AnalyticsPluginGateTests
    {
        private IAnalyticsService savedAnalytics;
        private IRevenueEventSink savedSink;
        private List<IAnalyticsBackend> savedBackends;
        private bool savedSuppressed;
        private AutomaticStartupProfile legacy;
        private PluginStartupProfile plugin;

        private static object InvokeHost(string method)
        {
            try
            {
                return typeof(AutomaticPluginHost).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            }
            catch (TargetInvocationException error)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        private static void InvokeDeclare()
        {
            try
            {
                var type = typeof(AnalyticsBootstrap).Assembly.GetType("Hung.Analytics.AnalyticsPlugin", true);
                var method = type.GetMethod("Declare", BindingFlags.NonPublic | BindingFlags.Static);
                method.Invoke(null, null);
            }
            catch (TargetInvocationException error)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        private void SetIds(PluginStartupProfile target, params string[] ids)
        {
            typeof(PluginStartupProfile)
                .GetField("disabledFeatureIds", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, new List<string>(ids));
        }

        [SetUp]
        public void SetUp()
        {
            savedAnalytics = Locator.Analytics;
            savedSink = Locator.RevenueSink;
            savedBackends = new List<IAnalyticsBackend>(AnalyticsBackends.Registered);
            AnalyticsBackends.Clear();
            savedSuppressed = AnalyticsBootstrap.EditorAutomaticStartupSuppressed;
            InvokeHost("ResetForPlay");
            legacy = ScriptableObject.CreateInstance<AutomaticStartupProfile>();
            plugin = ScriptableObject.CreateInstance<PluginStartupProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
            if (plugin != null) UnityEngine.Object.DestroyImmediate(plugin);
            InvokeHost("ResetForPlay");
            AnalyticsBootstrap.DiscardDeferred();
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go != null && go.name == "[HungAnalyticsTracking]")
                    UnityEngine.Object.DestroyImmediate(go);
            }
            AnalyticsBackends.Clear();
            foreach (var b in savedBackends) AnalyticsBackends.Register(b);
            if (savedAnalytics != null) ServiceSlots.Register<IAnalyticsService>(savedAnalytics);
            if (savedSink != null) ServiceSlots.Register<IRevenueEventSink>(savedSink);
            SessionState.SetBool(AnalyticsBootstrap.EditorSuppressionKey, savedSuppressed);
        }

        [Test]
        public void FeatureId_IsValidGrammar() =>
            Assert.IsTrue(PluginStartupProfile.IsValidFeatureId(AnalyticsPlugin.FeatureId));

        [TestCase(false, true,  false, ExpectedResult = true)]
        [TestCase(false, true,  true,  ExpectedResult = false)]
        [TestCase(false, false, false, ExpectedResult = false)]
        [TestCase(false, false, true,  ExpectedResult = false)]
        [TestCase(true,  true,  false, ExpectedResult = false)]
        [TestCase(true,  true,  true,  ExpectedResult = false)]
        [TestCase(true,  false, false, ExpectedResult = false)]
        [TestCase(true,  false, true,  ExpectedResult = false)]
        public bool Evaluate_TruthTable(bool editorSuppressed, bool legacyAnalytics, bool pluginDisabled)
        {
            legacy.legacyAnalytics = legacyAnalytics;
            SetIds(plugin, pluginDisabled ? new[] { "services.analytics" } : Array.Empty<string>());
            return AnalyticsPlugin.Evaluate(editorSuppressed, legacy, plugin);
        }

        [Test]
        public void Evaluate_BothProfilesAbsent_Starts() =>
            Assert.IsTrue(AnalyticsPlugin.Evaluate(false, null, null));

        [Test]
        public void Evaluate_OtherFeatureDisabled_Starts()
        {
            SetIds(plugin, "services.ads");
            Assert.IsTrue(AnalyticsPlugin.Evaluate(false, null, plugin));
        }

        [Test]
        public void Evaluate_EditorSuppressed_DoesNotTouchMalformedProfile()
        {
            SetIds(plugin, "Bad ID");
            Assert.IsFalse(AnalyticsPlugin.Evaluate(true, null, plugin));
        }

        [Test]
        public void Evaluate_MalformedPluginProfile_Throws()
        {
            SetIds(plugin, "Bad ID");
            Assert.Throws<InvalidOperationException>(() => AnalyticsPlugin.Evaluate(false, null, plugin));
        }

        [Test]
        public void Declare_EditorSuppressed_CountsAndDoesNotCollect()
        {
            SessionState.SetBool(AnalyticsBootstrap.EditorSuppressionKey, true);
            int beforeSuppressed = AnalyticsBootstrap.SuppressedAutomaticBootCount;
            InvokeDeclare();
            Assert.AreEqual(beforeSuppressed + 1, AnalyticsBootstrap.SuppressedAutomaticBootCount);
            var report = (FeatureBootReport)InvokeHost("StartCollected");
            Assert.AreEqual(0, report.Features.Count);
        }

        [Test]
        public void Declare_PluginPolicyOverrideDisabled_CountsAndDoesNotCollect()
        {
            SessionState.SetBool(AnalyticsBootstrap.EditorSuppressionKey, false);
            int beforeSuppressed = AnalyticsBootstrap.SuppressedAutomaticBootCount;
            using (PluginStartupPolicy.OverrideForTests("services.analytics", false))
            {
                InvokeDeclare();
            }
            Assert.AreEqual(beforeSuppressed + 1, AnalyticsBootstrap.SuppressedAutomaticBootCount);
            var report = (FeatureBootReport)InvokeHost("StartCollected");
            Assert.AreEqual(0, report.Features.Count);
        }

        [Test]
        public void Declare_PolicyThrows_LogsErrorAndDoesNotCollect()
        {
            Assert.AreEqual(0, Resources.LoadAll<PluginStartupProfile>("HungPluginStartup").Length);
            string folder1 = "Assets/__PluginStartupProof_1_" + Guid.NewGuid().ToString("N");
            string folder2 = "Assets/__PluginStartupProof_2_" + Guid.NewGuid().ToString("N");
            Assert.IsFalse(AssetDatabase.IsValidFolder(folder1));
            Assert.IsFalse(AssetDatabase.IsValidFolder(folder2));

            try
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder1));
                AssetDatabase.CreateFolder(folder1, "Resources");
                var p1 = ScriptableObject.CreateInstance<PluginStartupProfile>();
                string path1 = folder1 + "/Resources/HungPluginStartup.asset";
                AssetDatabase.CreateAsset(p1, path1);
                AssetDatabase.SaveAssetIfDirty(p1);
                AssetDatabase.ImportAsset(path1, ImportAssetOptions.ForceSynchronousImport);

                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder2));
                AssetDatabase.CreateFolder(folder2, "Resources");
                var p2 = ScriptableObject.CreateInstance<PluginStartupProfile>();
                string path2 = folder2 + "/Resources/HungPluginStartup.asset";
                AssetDatabase.CreateAsset(p2, path2);
                AssetDatabase.SaveAssetIfDirty(p2);
                AssetDatabase.ImportAsset(path2, ImportAssetOptions.ForceSynchronousImport);

                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"^\[Analytics\] automatic startup policy failed: "));
                SessionState.SetBool(AnalyticsBootstrap.EditorSuppressionKey, false);
                int beforeSuppressed = AnalyticsBootstrap.SuppressedAutomaticBootCount;
                InvokeDeclare();
                Assert.AreEqual(beforeSuppressed + 1, AnalyticsBootstrap.SuppressedAutomaticBootCount);
                var report = (FeatureBootReport)InvokeHost("StartCollected");
                Assert.AreEqual(0, report.Features.Count);
            }
            finally
            {
                if (AssetDatabase.IsValidFolder(folder1)) AssetDatabase.DeleteAsset(folder1);
                if (AssetDatabase.IsValidFolder(folder2)) AssetDatabase.DeleteAsset(folder2);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}


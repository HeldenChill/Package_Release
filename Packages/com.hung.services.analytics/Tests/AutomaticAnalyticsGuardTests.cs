using System;
using System.Reflection;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hung.Analytics.Tests
{
    public sealed class AutomaticAnalyticsGuardTests
    {
        [Test] public void ProductOptOut_ReturnsBeforeBackendLocatorOrTrackingWork()
        {
            bool suppression = SessionState.GetBool(AnalyticsBootstrap.EditorSuppressionKey, false);
            var oldAnalytics = Hung.Base.Locator.Analytics;
            var oldRevenue = Hung.Base.Locator.RevenueSink;
            int boot = AnalyticsBootstrap.AutomaticBootCount;
            int suppressed = AnalyticsBootstrap.SuppressedAutomaticBootCount;
            try
            {
                SessionState.SetBool(AnalyticsBootstrap.EditorSuppressionKey, false);
                using (var profile = new ProfileScope())
                {
                    typeof(AnalyticsBootstrap).Assembly.GetType("Hung.Analytics.AnalyticsPlugin", true)
                        .GetMethod("Declare", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                    Assert.That(AnalyticsBootstrap.AutomaticBootCount, Is.EqualTo(boot));
                    Assert.That(AnalyticsBootstrap.SuppressedAutomaticBootCount, Is.EqualTo(suppressed + 1));
                    Assert.That(Hung.Base.Locator.Analytics, Is.SameAs(oldAnalytics));
                    Assert.That(Hung.Base.Locator.RevenueSink, Is.SameAs(oldRevenue));
                }
            }
            finally { SessionState.SetBool(AnalyticsBootstrap.EditorSuppressionKey, suppression); }
        }

        private sealed class ProfileScope : IDisposable
        {
            private const string Root = "Assets/P2CStartupGuardTest";
            public ProfileScope()
            {
                if (Resources.Load<AutomaticStartupProfile>(AutomaticStartupPolicy.ResourcePath) != null || AssetDatabase.IsValidFolder(Root))
                    throw new InvalidOperationException("P2C_TEST_MUST_NOT_REPLACE_EXISTING_PROFILE");
                AssetDatabase.CreateFolder("Assets", "P2CStartupGuardTest");
                AssetDatabase.CreateFolder(Root, "Resources");
                var p = ScriptableObject.CreateInstance<AutomaticStartupProfile>();
                p.legacyPersistence = false; p.legacyAnalytics = false;
                AssetDatabase.CreateAsset(p, Root + "/Resources/HungAutomaticStartup.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (AutomaticStartupPolicy.ShouldStart(AutomaticStartupKind.LegacyPersistence))
                    throw new InvalidOperationException("P2C_TEST_PROFILE_NOT_VISIBLE");
            }
            public void Dispose()
            {
                if (!AssetDatabase.DeleteAsset(Root))
                    throw new InvalidOperationException("P2C_TEST_PROFILE_CLEANUP_FAILED");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Analytics.Tests
{
    public class AnalyticsDeferredStartupTests
    {
        IAnalyticsService _previousAnalytics;
        IRevenueEventSink _previousSink;

        [SetUp]
        public void SaveLocator()
        {
            _previousAnalytics = Locator.Analytics;
            _previousSink = Locator.RevenueSink;
            AnalyticsBootstrap.DiscardDeferred();
        }

        [TearDown]
        public void RestoreLocator()
        {
            AnalyticsBootstrap.DiscardDeferred();
            Locator.Analytics = _previousAnalytics;
            Locator.RevenueSink = _previousSink;
        }

        [Test]
        public void Hold_KeepsCallsWithEventTimeValues_AndReleaseReplaysInOrder()
        {
            var backend = new RecordingAnalyticsBackend();
            var service = new AnalyticsService(() => 0f);
            service.Hold();
            var args = new Dictionary<string, object> { { "n", 1 } };
            service.LogEvent("first", AnalyticsCategory.Product, args);
            args["n"] = 2;
            service.SetUserProperty("tier", "gold");
            service.OnRevenue("max", 0.5, "USD");
            service.LogEvent("second", AnalyticsCategory.Product);
            service.AddBackend(backend, AnalyticsCategory.All);
            Assert.IsEmpty(backend.Events, "Nothing is sent while holding.");

            service.ReleaseHeld();

            Assert.IsFalse(service.IsHolding);
            CollectionAssert.AreEqual(new[] { "first", "second" }, backend.Events.ConvertAll(e => e.Name));
            Assert.AreEqual(1, backend.Events[0].Parameters["n"], "Values are copied when the call is made.");
            Assert.AreEqual("gold", backend.UserProperties["tier"]);
            Assert.AreEqual(1, backend.Revenues.Count);
        }

        [Test]
        public void Hold_OverLimit_DropsExtraCallsAndWarnsOnRelease()
        {
            var backend = new RecordingAnalyticsBackend();
            var service = new AnalyticsService(() => 0f);
            service.Hold();
            for (int i = 0; i < AnalyticsService.MaxHeldCalls + 3; i++) service.LogEvent("e" + i, AnalyticsCategory.Product);
            service.AddBackend(backend, AnalyticsCategory.All);
            LogAssert.Expect(LogType.Warning, new Regex("3 calls over the 512 hold limit were dropped"));

            service.ReleaseHeld();

            Assert.AreEqual(AnalyticsService.MaxHeldCalls, backend.Events.Count);
            Assert.AreEqual("e0", backend.Events[0].Name);
        }

        [Test]
        public void DiscardHeld_SendsNothing_AndStopsHolding()
        {
            var backend = new RecordingAnalyticsBackend();
            var service = new AnalyticsService(() => 0f);
            service.Hold();
            service.LogEvent("held", AnalyticsCategory.Product);
            service.AddBackend(backend, AnalyticsCategory.All);

            service.DiscardHeld();
            service.LogEvent("after", AnalyticsCategory.Product);

            Assert.IsFalse(service.IsHolding);
            CollectionAssert.AreEqual(new[] { "after" }, backend.Events.ConvertAll(e => e.Name));
        }

        [Test]
        public void InstallDeferred_StartsNoSdkUntilStartDeferred_ThenPassesLaunchId()
        {
            var backend = new RecordingAnalyticsBackend();
            var service = AnalyticsBootstrap.InstallDeferred(null, new IAnalyticsBackend[] { backend }, () => 0f);
            Assert.AreSame(service, Locator.Analytics);
            Assert.AreSame(service, Locator.RevenueSink);
            Assert.IsTrue(AnalyticsBootstrap.IsDeferred);
            Locator.Analytics.LogEvent("early", AnalyticsCategory.Product);
            Assert.IsNull(backend.InitializedWith, "No SDK starts before consent.");

            Assert.IsTrue(AnalyticsBootstrap.StartDeferred("launch-id"));

            Assert.IsNotNull(backend.InitializedWith);
            Assert.AreEqual("launch-id", AnalyticsBootstrap.LaunchUserId);
            CollectionAssert.AreEqual(new[] { "early" }, backend.Events.ConvertAll(e => e.Name));
            Assert.IsFalse(AnalyticsBootstrap.IsDeferred);
            Assert.IsFalse(AnalyticsBootstrap.StartDeferred("again"), "Starts once.");
        }

        [Test]
        public void DiscardDeferred_DropsHeldCalls_AndStartsNoSdk()
        {
            var backend = new RecordingAnalyticsBackend();
            AnalyticsBootstrap.InstallDeferred(null, new IAnalyticsBackend[] { backend }, () => 0f);
            Locator.Analytics.LogEvent("early", AnalyticsCategory.Product);

            AnalyticsBootstrap.DiscardDeferred();
            Locator.Analytics.LogEvent("later", AnalyticsCategory.Product);

            Assert.IsNull(backend.InitializedWith);
            Assert.IsEmpty(backend.Events);
            Assert.IsFalse(AnalyticsBootstrap.IsDeferred);
            Assert.IsFalse(AnalyticsBootstrap.StartDeferred("id"));
        }

        [Test]
        public void StartDeferred_BackendThatThrows_IsSkipped_OthersStart()
        {
            var bad = new RecordingAnalyticsBackend("bad") { ThrowOnInitialize = new System.InvalidOperationException("no key") };
            var good = new RecordingAnalyticsBackend("good");
            AnalyticsBootstrap.InstallDeferred(null, new IAnalyticsBackend[] { bad, good }, () => 0f);
            LogAssert.Expect(LogType.Error, new Regex("bad init failed"));

            AnalyticsBootstrap.StartDeferred(null);
            Locator.Analytics.LogEvent("x", AnalyticsCategory.Product);

            Assert.IsEmpty(bad.Events);
            Assert.IsFalse(AnalyticsBootstrap.IsBackendStarted("bad"));
            Assert.IsTrue(AnalyticsBootstrap.IsBackendStarted("good"));
            CollectionAssert.AreEqual(new[] { "x" }, good.Events.ConvertAll(e => e.Name));
            Assert.IsNull(AnalyticsBootstrap.LaunchUserId);
        }

        [Test]
        public void Settings_DeferInitialization_DefaultsOff()
        {
            var settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
            try { Assert.IsFalse(settings.deferInitialization); }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void AllBackendsFail_CannotReportSuccessfulStartup()
        {
            var bad = new RecordingAnalyticsBackend("appmetrica") { ThrowOnInitialize = new System.InvalidOperationException("no key") };
            var service = AnalyticsBootstrap.InstallDeferred(null, new IAnalyticsBackend[] { bad }, () => 0f);
            LogAssert.Expect(LogType.Error, new Regex("appmetrica init failed"));
            Assert.IsTrue(AnalyticsBootstrap.StartDeferred("test-id"), "State transition alone is not a startup verdict.");
            Assert.IsFalse(service.IsHolding);
            Assert.AreEqual(0, service.BackendCount);
            Assert.IsFalse(AnalyticsBootstrap.IsBackendStarted("appmetrica"));
            Assert.IsFalse(AnalyticsBootstrap.IsBackendReady("appmetrica"));
            Assert.IsFalse(AnalyticsBootstrap.IsIdentityApplied("appmetrica"));
        }

        [Test]
        public void ReadySignal_FromPreviousStartup_CannotMarkThisStartupReady()
        {
            AnalyticsBootstrap.InstallDeferred(null, new IAnalyticsBackend[0], () => 0f);
            var oldSignal = AnalyticsBootstrap.ReadySignal("firebase");
            AnalyticsBootstrap.DiscardDeferred();
            AnalyticsBootstrap.InstallDeferred(null, new IAnalyticsBackend[0], () => 0f);
            oldSignal(true);
            Assert.IsFalse(AnalyticsBootstrap.IsBackendReady("firebase"));
            Assert.IsFalse(AnalyticsBootstrap.IsIdentityApplied("firebase"));
            AnalyticsBootstrap.ReadySignal("firebase")(false);
            Assert.IsTrue(AnalyticsBootstrap.IsBackendReady("firebase"));
            Assert.IsFalse(AnalyticsBootstrap.IsIdentityApplied("firebase"));
        }
    }
}

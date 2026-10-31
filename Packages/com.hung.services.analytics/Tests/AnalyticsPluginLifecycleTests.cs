using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Hung.Analytics.Tests.Tracking;
using Hung.Analytics.Tracking;
using Hung.Base;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hung.Analytics.Tests
{
    public sealed class AnalyticsPluginLifecycleTests
    {
        private IAnalyticsService savedAnalytics;
        private IRevenueEventSink savedSink;
        private List<IAnalyticsBackend> savedBackends;
        private bool savedSuppressed;

        private sealed class ForeignAnalyticsService : IAnalyticsService
        {
            public void LogEvent(string name, AnalyticsCategory category, IReadOnlyDictionary<string, object> parameters = null) { }
            public void SetUserProperty(string key, string value) { }
            public void AdsRewardOffer(Placement place) { }
            public void AdsRewardClick(Placement place) { }
            public void AdsRewardShow(Placement place) { }
            public void AdsRewardShowFail(Placement place, string error) { }
            public void AdsRewardComplete(Placement place, string type) { }
            public void AdsRewardLoadComplete() { }
            public void AdsRewardLoad() { }
            public void AdsRewardLoadFail() { }
            public void AdsInterFail(string error) { }
            public void AdsInterLoad() { }
            public void AdsInterShow(Placement place) { }
            public void AdsInterClick() { }
            public void AdsInterLoadComplete() { }
            public void AdsInterComplete() { }
            public void BuyIAPComplete(IAP_ITEM item, Placement placement) { }
            public void TutorialStep(string name, int step) { }
            public void LevelTrackEvent(LEVEL_STATE state, int level, int stars, bool firstPass, bool isFtu) { }
            public void EarnVirtualCurrency(string name, long value, string source) { }
            public void SpendVirtualCurrency(string name, long value, string itemName) { }
        }

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

        [SetUp]
        public void SetUp()
        {
            savedAnalytics = Locator.Analytics;
            savedSink = Locator.RevenueSink;
            savedBackends = new List<IAnalyticsBackend>(AnalyticsBackends.Registered);
            AnalyticsBackends.Clear();
            savedSuppressed = AnalyticsBootstrap.EditorAutomaticStartupSuppressed;
            InvokeHost("ResetForPlay");
        }

        [TearDown]
        public void TearDown()
        {
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
        public void HostStart_ReportsReadyRowAndPublishesSlots()
        {
            int beforeBoot = AnalyticsBootstrap.AutomaticBootCount;
            AutomaticPluginHost.Collect(AnalyticsPlugin.CreateRegistration());
            var report = (FeatureBootReport)InvokeHost("StartCollected");

            Assert.AreEqual(1, report.Features.Count);
            var row = report.Features[0];
            Assert.AreEqual("services.analytics", row.Id);
            Assert.AreEqual(FeatureState.Ready, row.State);
            Assert.IsTrue(report.ProductReady);

            Assert.IsNotNull(Locator.Analytics);
            Assert.AreSame(Locator.Analytics, Locator.RevenueSink);
            Assert.AreEqual(beforeBoot + 1, AnalyticsBootstrap.AutomaticBootCount);

            var trackingSettings = Resources.Load<TrackingSettings>(TrackingSettings.ResourcePath);
            bool trackingEnabled = trackingSettings == null || trackingSettings.enabled;
            int trackingObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Count(x => x != null && x.scene.IsValid() && x.name == "[HungAnalyticsTracking]");
            Assert.AreEqual(trackingEnabled ? 1 : 0, trackingObjects);
        }

        [Test]
        public void HostShutdown_ReleasesSlotsRunnerAndTracking()
        {
            AutomaticPluginHost.Collect(AnalyticsPlugin.CreateRegistration());
            InvokeHost("StartCollected");

            AutomaticPluginHost.Shutdown();

            Assert.IsFalse(ServiceSlots.TryGet<IAnalyticsService>(out _));
            Assert.IsFalse(ServiceSlots.TryGet<IRevenueEventSink>(out _));
            Assert.IsNull(TrackingRunner.Current);

            int trackingObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Count(x => x != null && x.scene.IsValid() && x.name == "[HungAnalyticsTracking]");
            Assert.AreEqual(0, trackingObjects);

            var row = AutomaticPluginHost.GetReport().Features.First(f => f.Id == "services.analytics");
            Assert.AreEqual(FeatureState.Stopped, row.State);
        }

        [Test]
        public void Shutdown_Twice_IsNoOp()
        {
            AutomaticPluginHost.Collect(AnalyticsPlugin.CreateRegistration());
            InvokeHost("StartCollected");

            Assert.DoesNotThrow(() =>
            {
                AutomaticPluginHost.Shutdown();
                AutomaticPluginHost.Shutdown();
            });

            Assert.IsFalse(ServiceSlots.TryGet<IAnalyticsService>(out _));
            Assert.IsFalse(ServiceSlots.TryGet<IRevenueEventSink>(out _));
            Assert.IsNull(TrackingRunner.Current);

            var row = AutomaticPluginHost.GetReport().Features.First(f => f.Id == "services.analytics");
            Assert.AreEqual(FeatureState.Stopped, row.State);
        }

        [Test]
        public void Initialize_ForeignOwner_FailsRowAndKeepsForeignSlot()
        {
            int beforeBoot = AnalyticsBootstrap.AutomaticBootCount;
            var foreign = new ForeignAnalyticsService();
            ServiceSlots.Register<IAnalyticsService>(foreign);

            AutomaticPluginHost.Collect(AnalyticsPlugin.CreateRegistration());
            var report = (FeatureBootReport)InvokeHost("StartCollected");

            var row = report.Features.First(f => f.Id == "services.analytics");
            Assert.AreEqual(FeatureState.Failed, row.State);
            Assert.AreEqual("FEATURE_INITIALIZATION_EXCEPTION", row.DiagnosticCode);
            Assert.IsTrue(report.ProductReady);
            Assert.AreSame(foreign, ServiceSlots.Get<IAnalyticsService>());
            Assert.AreEqual(beforeBoot + 1, AnalyticsBootstrap.AutomaticBootCount);
        }

        [Test]
        public void Shutdown_AfterPartialStart_ReleasesSlotsWithoutThrowing()
        {
            var s = AnalyticsBootstrap.Install(null, Array.Empty<IAnalyticsBackend>());
            Assert.DoesNotThrow(() =>
            {
                AnalyticsBootstrap.Uninstall(s);
                AnalyticsBootstrap.Uninstall(s);
                AnalyticsBootstrap.Uninstall(null);
            });
            Assert.IsFalse(ServiceSlots.TryGet<IAnalyticsService>(out _));
            Assert.IsFalse(ServiceSlots.TryGet<IRevenueEventSink>(out _));
        }

        [Test]
        public void Uninstall_OtherInstanceInSlot_LeavesItRegistered()
        {
            var a = AnalyticsBootstrap.Install(null, Array.Empty<IAnalyticsBackend>());
            var b = AnalyticsBootstrap.Install(null, Array.Empty<IAnalyticsBackend>());
            AnalyticsBootstrap.Uninstall(a);
            Assert.AreSame(b, Locator.Analytics);
        }

        [Test]
        public void Uninstall_DeferredSession_DiscardsHold()
        {
            var s = AnalyticsBootstrap.InstallDeferred(null, Array.Empty<IAnalyticsBackend>(), () => 0f);
            Assert.IsTrue(AnalyticsBootstrap.IsDeferred);
            Assert.IsTrue(s.IsHolding);
            AnalyticsBootstrap.Uninstall(s);
            Assert.IsFalse(AnalyticsBootstrap.IsDeferred);
            Assert.IsFalse(s.IsHolding);
        }

        [Test]
        public void DestroyCurrent_DispatchesBlurOnceThenDestroys()
        {
            var spy = new SpyRule();
            var harness = new TrackingHarness(_ => new IRule[] { spy });
            harness.Launch();
            Assert.IsTrue(harness.Pipeline.Started);

            var runner = TrackingRunner.Create(harness.Pipeline);
            Assert.AreSame(runner, TrackingRunner.Current);

            TrackingRunner.DestroyCurrent();
            TrackingRunner.DestroyCurrent();

            Assert.AreEqual(1, spy.Seen.Count(k => k == FactKind.Blur));
            Assert.IsNull(TrackingRunner.Current);
            Assert.IsTrue(runner == null);
        }

        [Test]
        public void DestroyCurrent_UnstartedPipeline_DispatchesNoBlur()
        {
            var spy = new SpyRule();
            var harness = new TrackingHarness(_ => new IRule[] { spy });
            var service = new AnalyticsService(() => 0f);
            service.AddBackend(harness.Backend, AnalyticsCategory.All);
            var ctx = new TrackingContext(harness.Settings, harness.Clock);
            var pipeline = new TrackingPipeline(ctx, new EventEmitter(service, harness.Settings, harness.Warnings.Add), harness.Store, harness.Warnings.Add);
            pipeline.Add(spy);
            Assert.IsFalse(pipeline.Started);

            var runner = TrackingRunner.Create(pipeline);
            Assert.AreSame(runner, TrackingRunner.Current);

            TrackingRunner.DestroyCurrent();

            Assert.AreEqual(0, spy.Seen.Count(k => k == FactKind.Blur));
            Assert.IsNull(TrackingRunner.Current);
            Assert.IsTrue(runner == null);
        }
    }
}

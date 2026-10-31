using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using NUnit.Framework;

namespace Hung.Analytics.Tests
{
    public sealed class AnalyticsSlotOwnershipTests
    {
        private IAnalyticsService savedAnalytics;
        private IRevenueEventSink savedSink;
        private sealed class ForeignSink : IRevenueEventSink
        {
            public void OnRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra = null) { }
        }
        private sealed class Backend : IAnalyticsBackend
        {
            public int Starts;
            public string Id => "slot-proof";
            public void Initialize(AnalyticsBackendEntry entry, bool debugLog) => Starts++;
            public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters) { }
            public void SetUserProperty(string key, string value) { }
            public void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra) { }
        }
        [SetUp]
        public void SetUp()
        {
            savedAnalytics = Locator.Analytics; savedSink = Locator.RevenueSink;
            ServiceSlots.Unregister<IAnalyticsService>(savedAnalytics);
            ServiceSlots.Unregister<IRevenueEventSink>(savedSink);
            AnalyticsBootstrap.DiscardDeferred();
        }
        [TearDown]
        public void TearDown()
        {
            AnalyticsBootstrap.DiscardDeferred();
            ServiceSlots.Unregister<IAnalyticsService>(Locator.Analytics);
            ServiceSlots.Unregister<IRevenueEventSink>(Locator.RevenueSink);
            if (savedAnalytics != null) ServiceSlots.Register<IAnalyticsService>(savedAnalytics);
            if (savedSink != null) ServiceSlots.Register<IRevenueEventSink>(savedSink);
        }
        [Test]
        public void SecondSlotConflict_PreventsBackendStartAndPreservesBothSlots()
        {
            var original = new AnalyticsService(); var foreign = new ForeignSink(); var backend = new Backend();
            ServiceSlots.Register<IAnalyticsService>(original);
            ServiceSlots.Register<IRevenueEventSink>(foreign);
            Assert.Throws<InvalidOperationException>(() => AnalyticsBootstrap.Install(null, new[] { backend }));
            Assert.AreEqual(0, backend.Starts);
            Assert.AreSame(original, Locator.Analytics); Assert.AreSame(foreign, Locator.RevenueSink);
        }
        [Test]
        public void DeferredSecondSlotConflict_DoesNotCreateDeferredSession()
        {
            var foreign = new ForeignSink(); ServiceSlots.Register<IRevenueEventSink>(foreign);
            Assert.Throws<InvalidOperationException>(() => AnalyticsBootstrap.InstallDeferred(null, Array.Empty<IAnalyticsBackend>()));
            Assert.IsFalse(AnalyticsBootstrap.IsDeferred); Assert.IsNull(Locator.Analytics);
        }
        [Test]
        public void FailureAfterPublication_RestoresPreviousReferencesAndDeferredSession()
        {
            var previous = AnalyticsBootstrap.InstallDeferred(null, Array.Empty<IAnalyticsBackend>(), () => 0f);
            var attempted = new AnalyticsService();
            var publish = typeof(AnalyticsBootstrap).GetMethod("PublishPair", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(publish);
            var failure = Assert.Throws<TargetInvocationException>(() => publish.Invoke(null,
                new object[] { attempted, new Action(() => throw new InvalidOperationException("forced")) }));
            Assert.AreEqual("forced", failure.InnerException.Message);
            Assert.AreSame(previous, Locator.Analytics); Assert.AreSame(previous, Locator.RevenueSink);
            Assert.IsTrue(AnalyticsBootstrap.IsDeferred); Assert.IsTrue(previous.IsHolding);
        }
        [Test]
        public void SuccessfulReplacement_RetiresOldProxyAndPublishesOneInstance()
        {
            var previous = AnalyticsBootstrap.InstallDeferred(null, Array.Empty<IAnalyticsBackend>(), () => 0f);
            var current = AnalyticsBootstrap.InstallDeferred(null, Array.Empty<IAnalyticsBackend>(), () => 0f);
            Assert.IsFalse(previous.IsHolding); Assert.AreEqual(0, previous.BackendCount);
            Assert.AreSame(current, Locator.Analytics); Assert.AreSame(current, Locator.RevenueSink);
        }
    }
}

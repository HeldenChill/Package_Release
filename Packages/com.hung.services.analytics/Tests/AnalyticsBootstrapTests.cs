using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Analytics.Tests
{
    public class AnalyticsBootstrapTests
    {
        IAnalyticsService _previousAnalytics;
        IRevenueEventSink _previousSink;

        [SetUp]
        public void SaveLocator()
        {
            _previousAnalytics = Locator.Analytics;
            _previousSink = Locator.RevenueSink;
            AnalyticsBackends.Clear();
        }

        [TearDown]
        public void RestoreLocator()
        {
            Locator.Analytics = _previousAnalytics;
            Locator.RevenueSink = _previousSink;
            AnalyticsBackends.Clear();
        }

        [Test]
        public void Install_UsesSettingsCategories_AndRegistersLocator()
        {
            var settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
            settings.backends = new List<AnalyticsBackendEntry>
            {
                new AnalyticsBackendEntry("rec", AnalyticsCategory.Design) { apiKey = "k" }
            };
            var backend = new RecordingAnalyticsBackend("rec");

            var service = AnalyticsBootstrap.Install(settings, new IAnalyticsBackend[] { backend });
            service.LogEvent("ads_only", AnalyticsCategory.Ads);
            service.LogEvent("design", AnalyticsCategory.Design);

            Assert.AreSame(service, Locator.Analytics);
            Assert.AreSame(service, Locator.RevenueSink);
            Assert.AreEqual("k", backend.InitializedWith.apiKey);
            CollectionAssert.AreEqual(new[] { "design" }, backend.Events.ConvertAll(e => e.Name));
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void Install_BackendInitThrows_IsExcluded_OthersStillWork()
        {
            var broken = new RecordingAnalyticsBackend(AnalyticsBackendIds.AppsFlyer)
            {
                ThrowOnInitialize = new InvalidOperationException("dev key empty")
            };
            var healthy = new RecordingAnalyticsBackend(AnalyticsBackendIds.Firebase);

            LogAssert.Expect(LogType.Error, new Regex("appsflyer init failed"));
            var service = AnalyticsBootstrap.Install(null, new IAnalyticsBackend[] { broken, healthy });
            service.LogEvent("x", AnalyticsCategory.Product);

            Assert.AreEqual(1, service.BackendCount);
            Assert.AreEqual(0, broken.Events.Count);
            Assert.AreEqual(1, healthy.Events.Count);
        }

        [Test]
        public void Install_NoBackends_LocatorIsStillUsable()
        {
            var service = AnalyticsBootstrap.Install(null, Array.Empty<IAnalyticsBackend>());

            Assert.AreEqual(0, service.BackendCount);
            Assert.DoesNotThrow(() => Locator.Analytics.AdsInterClick());
        }

        [Test]
        public void Registry_SameIdTwice_KeepsOne()
        {
            AnalyticsBackends.Register(new RecordingAnalyticsBackend("dup"));
            AnalyticsBackends.Register(new RecordingAnalyticsBackend("dup"));

            Assert.AreEqual(1, AnalyticsBackends.Registered.Count);
        }
    }
}

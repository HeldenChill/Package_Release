using System.Linq;
using Hung.Analytics.Tracking;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Analytics.Tests.Tracking
{
    public class TrackingFacadeTests
    {
        AnalyticsService _service;
        RecordingAnalyticsBackend _backend;
        TrackingSettings _settings;

        [SetUp]
        public void Build()
        {
            AnalyticsTracking.Reset();
            _service = new AnalyticsService(() => 0f);
            _backend = new RecordingAnalyticsBackend();
            _service.AddBackend(_backend, AnalyticsCategory.All);
            _settings = TrackingSettings.CreateDefault();
        }

        [TearDown]
        public void Teardown()
        {
            AnalyticsTracking.Reset();
            Object.DestroyImmediate(_settings);
        }

        TrackingPipeline Install()
        {
            var p = TrackingBootstrap.Install(_service, _settings, new InMemoryTrackingStateStore(), new FakeClock());
            p?.Start();
            return p;
        }

        [Test]
        public void Facts_BeforeInstall_IsNoOp()
        {
            Assert.DoesNotThrow(() =>
            {
                AnalyticsTracking.Facts.StageStart(1, false);
                AnalyticsTracking.Facts.Feature("x", ("k", 1));
            });
            Assert.IsEmpty(_backend.Events);
        }

        [Test]
        public void Install_ColdStartAndFactsReachBackends()
        {
            Install();
            AnalyticsTracking.Facts.StageStart(3, false);
            var names = _backend.Events.Select(e => e.Name).ToList();
            Assert.Contains("ftu_open_app", names);
            Assert.Contains("ftu_stage_start", names);
            Assert.AreEqual("1", _backend.UserProperties["ftu"]);
        }

        [Test]
        public void RegisterBeforeInstall_RuleSeesColdStart()
        {
            var spy = new SpyRule();
            AnalyticsTracking.Register(spy);
            Install();
            Assert.AreEqual(FactKind.ColdStart, spy.Seen.First());
        }

        [Test]
        public void RewardedAdComplete_ResolvesPendingFeelingPositive()
        {
            Install();
            AnalyticsTracking.Facts.Gacha("pet", 1, "gem", 100);
            _service.AdsRewardComplete(Placement.IN_GAME, "reward");
            var feeling = _backend.Events.Last(e => e.Name == "ftu_gacha_feeling");
            Assert.AreEqual("positive", feeling.Parameters["type"]);
        }

        [Test]
        public void Disabled_InstallsNoOp()
        {
            _settings.enabled = false;
            Assert.IsNull(Install());
            AnalyticsTracking.Facts.StageStart(1, false);
            Assert.IsEmpty(_backend.Events);
        }

        [Test]
        public void Feature_ParamsTupleBecomesArgs()
        {
            _settings.featureEvents.Add("skill_pick");
            Install();
            AnalyticsTracking.Facts.Feature("skill_pick", ("skill_id", "fire_ring"));
            Assert.AreEqual("fire_ring", _backend.Events.Last(e => e.Name == "ftu_skill_pick").Parameters["skill_id"]);
        }
    }
}

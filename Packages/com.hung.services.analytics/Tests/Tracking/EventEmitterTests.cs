using System.Collections.Generic;
using System.Linq;
using Hung.Analytics.Tracking;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Analytics.Tests.Tracking
{
    public class EventEmitterTests
    {
        RecordingAnalyticsBackend _design;
        RecordingAnalyticsBackend _ads;
        TrackingSettings _settings;
        List<string> _warnings;
        EventEmitter _emit;

        [SetUp]
        public void Build()
        {
            var service = new AnalyticsService(() => 0f);
            _design = new RecordingAnalyticsBackend("design");
            _ads = new RecordingAnalyticsBackend("ads");
            service.AddBackend(_design, AnalyticsCategory.Design);
            service.AddBackend(_ads, AnalyticsCategory.Ads);
            _settings = TrackingSettings.CreateDefault();
            _warnings = new List<string>();
            _emit = new EventEmitter(service, _settings, _warnings.Add);
        }

        [TearDown] public void Destroy() => Object.DestroyImmediate(_settings);

        static Dictionary<string, object> Args(params (string k, object v)[] kv) => kv.ToDictionary(p => p.k, p => p.v);

        [Test]
        public void AMode_SendsNameAndParams_AsDesign()
        {
            _emit.Declare("stage_start", "stage_{stage}_start", 100);
            _emit.Emit("stage_start", false, Args(("stage", 12), ("replay", 0)));
            var e = _design.Events.Single();
            Assert.AreEqual("stage_start", e.Name);
            Assert.AreEqual(12, e.Parameters["stage"]);
            Assert.AreEqual(0, _ads.Events.Count);
        }

        [Test]
        public void Ftu_AddsPrefix()
        {
            _emit.Emit("open_app", true, null);
            Assert.AreEqual("ftu_open_app", _design.Events.Single().Name);
        }

        [Test]
        public void EmitTagged_NoPrefix_AddsFtuParam()
        {
            _emit.EmitTagged("tutorial", true, Args(("id", "Tut_3")));
            var e = _design.Events.Single();
            Assert.AreEqual("tutorial", e.Name);
            Assert.AreEqual(1, e.Parameters["ftu"]);
        }

        [Test]
        public void BMode_RendersTemplate_AndRemovesUsedParams()
        {
            _settings.modeOverrides.Add(new ModeOverride { eventName = "stage_start", mode = OutputMode.B });
            _emit.Declare("stage_start", "stage_{stage}_start", 100);
            _emit.Emit("stage_start", true, Args(("stage", 12), ("replay", 0)));
            var e = _design.Events.Single();
            Assert.AreEqual("ftu_stage_12_start", e.Name);
            Assert.IsFalse(e.Parameters.ContainsKey("stage"));
            Assert.AreEqual(0, e.Parameters["replay"]);
        }

        [Test]
        public void BMode_LowercasesRenderedValues()
        {
            _settings.defaultMode = OutputMode.B;
            _emit.Declare("gacha", "gacha_{pool}_{type}", 16);
            _emit.Emit("gacha", false, Args(("pool", "Pet"), ("type", "Rush")));
            Assert.AreEqual("gacha_pet_rush", _design.Events.Single().Name);
        }

        [Test]
        public void BMode_Over40Chars_FallsBackToA_WithWarning()
        {
            _settings.defaultMode = OutputMode.B;
            _settings.bNameBudget = 100000;
            _emit.Declare("consecutive_fail_heat", "consecutive_fail_stage_{stage}_heat_{heat}", 400);
            _emit.Emit("consecutive_fail_heat", true, Args(("stage", 100), ("heat", "chill")));
            var e = _design.Events.Single();
            Assert.AreEqual("ftu_consecutive_fail_heat", e.Name);
            Assert.AreEqual(100, e.Parameters["stage"]);
            Assert.AreEqual(1, _warnings.Count);
        }

        [Test]
        public void BMode_TemplateHoleWithoutParam_FallsBackToA()
        {
            _settings.defaultMode = OutputMode.B;
            _emit.Declare("stage_fail", "stage_{stage}_fail_wave_{wave}", 2000);
            _settings.bNameBudget = 100000;
            _emit.Emit("stage_fail", false, Args(("stage", 3)));
            Assert.AreEqual("stage_fail", _design.Events.Single().Name);
        }

        [Test]
        public void Budget_ForcesLargestBEventsToA()
        {
            _settings.defaultMode = OutputMode.B;
            _settings.bNameBudget = 10;
            _emit.Declare("big", "big_{n}", 100);
            _emit.Declare("small", "small_{n}", 2);
            Assert.AreEqual(OutputMode.A, _emit.ModeOf("big"));
            Assert.AreEqual(OutputMode.B, _emit.ModeOf("small"));
            Assert.AreEqual(1, _warnings.Count);
        }

        [Test]
        public void NullTemplate_AlwaysA()
        {
            _settings.defaultMode = OutputMode.B;
            _emit.Declare("login_day", null, 0);
            Assert.AreEqual(OutputMode.A, _emit.ModeOf("login_day"));
        }

        [Test]
        public void SetUserProperty_ReachesBackends()
        {
            _emit.SetUserProperty("ftu", "1");
            Assert.AreEqual("1", _design.UserProperties["ftu"]);
        }
    }
}

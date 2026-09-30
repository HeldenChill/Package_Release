using System.Collections.Generic;
using System.Linq;
using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class FeatureRulesTests
    {
        static TrackingHarness New()
        {
            var h = new TrackingHarness(_ => new IRule[] { new FeatureRule(), new TutorialRule() });
            h.Settings.featureEvents.AddRange(new[] { "skill_pick", "milestone_claim", "x2_speed_on" });
            return h;
        }

        static IReadOnlyDictionary<string, object> Args(params (string k, object v)[] kv) => kv.ToDictionary(p => p.k, p => p.v);

        [Test]
        public void Feature_InStage_AddsStageAndReplay()
        {
            var h = New().Launch();
            h.Do(Fact.StageStart(12, false));
            h.Do(Fact.Feature("skill_pick", Args(("skill_id", "fire_ring"))));
            var e = h.Last("ftu_skill_pick");
            Assert.AreEqual(12, e.Parameters["stage"]);
            Assert.AreEqual(0, e.Parameters["replay"]);
            Assert.AreEqual("fire_ring", e.Parameters["skill_id"]);
        }

        [Test]
        public void Feature_AtHome_ExplicitArgsOnly()
        {
            var h = New().Launch();
            h.Do(Fact.Feature("milestone_claim", Args(("stage", 10), ("current_stage", 16))));
            var e = h.Last("ftu_milestone_claim");
            Assert.AreEqual(10, e.Parameters["stage"]);
            Assert.IsFalse(e.Parameters.ContainsKey("replay"));
        }

        [Test]
        public void Feature_UnlistedId_EmitsNothing()
        {
            var h = New().Launch();
            h.Clear();
            h.Do(Fact.Feature("pet_upgrade", null));
            Assert.IsEmpty(h.Names);
        }

        [Test]
        public void Tutorial_NoPrefix_FtuParam_SkippedOnlyWhenTrue()
        {
            var h = New().Launch();
            h.Do(Fact.Tutorial("Tut_3", TutorialPhase.Start, false));
            var start = h.Last("tutorial");
            Assert.AreEqual("Tut_3", start.Parameters["id"]);
            Assert.AreEqual("start", start.Parameters["step"]);
            Assert.AreEqual(1, start.Parameters["ftu"]);
            Assert.IsFalse(start.Parameters.ContainsKey("skipped"));
            h.Kill(5);
            h.Do(Fact.Tutorial("Tut_opt_3", TutorialPhase.End, true));
            var end = h.Last("tutorial");
            Assert.AreEqual(0, end.Parameters["ftu"]);
            Assert.AreEqual(1, end.Parameters["skipped"]);
        }

        [Test]
        public void StandardRules_ContainsTheWholePack()
        {
            var names = StandardRules.Create().Select(r => r.GetType().Name).ToList();
            CollectionAssert.AreEqual(new[]
            {
                "OpenAppRule", "LoginDayRule", "FtuTimeplayRule", "StageLifecycleRule", "StageCompleteRule", "CheckpointRule",
                "FailStreakRule", "FailHeatRule", "WinStreakRule", "GachaRule", "GachaFeelingRule", "FeatureRule", "TutorialRule"
            }, names);
        }
    }
}

using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class StreakRulesTests
    {
        static TrackingHarness New() => new TrackingHarness(_ => new IRule[]
        {
            new FailStreakRule(), new FailHeatRule(), new WinStreakRule()
        });

        static void Attempt(TrackingHarness h, int stage, StageResult result, int wave = 1)
        {
            h.Do(Fact.StageStart(stage, false));
            h.Do(Fact.WaveReached(wave));
            h.Do(Fact.StageEnd(result));
        }

        [Test]
        public void FailStreak_PerStage_TypesByN_ResetOnlyByWinOnThatStage()
        {
            var h = New().Launch();
            Attempt(h, 12, StageResult.Fail);
            Assert.IsFalse(h.Names.Contains("ftu_consecutive_fail"));
            Attempt(h, 12, StageResult.Fail);
            Assert.AreEqual("normal", h.Last("ftu_consecutive_fail").Parameters["type"]);
            Attempt(h, 11, StageResult.Win);
            Attempt(h, 12, StageResult.Fail);
            var e = h.Last("ftu_consecutive_fail");
            Assert.AreEqual(3, e.Parameters["n"]);
            Assert.AreEqual("rage", e.Parameters["type"]);
            Attempt(h, 12, StageResult.Fail);
            Assert.AreEqual("frustrated", h.Last("ftu_consecutive_fail").Parameters["type"]);
            Attempt(h, 12, StageResult.Win);
            h.Clear();
            Attempt(h, 12, StageResult.Fail);
            Assert.IsFalse(h.Names.Contains("ftu_consecutive_fail"));
        }

        [Test]
        public void FailWave_SameWaveRun_RestartsOnOtherWave()
        {
            var h = New().Launch();
            Attempt(h, 12, StageResult.Fail, 5);
            Attempt(h, 12, StageResult.Fail, 5);
            var e = h.Last("ftu_consecutive_fail_wave");
            Assert.AreEqual(5, e.Parameters["wave"]);
            Assert.AreEqual(2, e.Parameters["n"]);
            h.Clear();
            Attempt(h, 12, StageResult.Fail, 7);
            Assert.IsFalse(h.Names.Contains("ftu_consecutive_fail_wave"));
            Attempt(h, 12, StageResult.Fail, 7);
            Assert.AreEqual(7, h.Last("ftu_consecutive_fail_wave").Parameters["wave"]);
        }

        [Test]
        public void Heat_MeasuredFromFailToRetryStart()
        {
            var h = New().Launch();
            Attempt(h, 12, StageResult.Fail);
            h.Clock.Advance(4);
            h.Do(Fact.StageStart(12, false));
            var hot = h.Last("ftu_consecutive_fail_heat");
            Assert.AreEqual("hot", hot.Parameters["heat"]);
            Assert.AreEqual(1, hot.Parameters["n"]);
            Assert.AreEqual(4, hot.Parameters["gap_min"]);
            h.Do(Fact.StageEnd(StageResult.Fail));
            h.Clock.Advance(110);
            h.Do(Fact.StageStart(12, false));
            var cold = h.Last("ftu_consecutive_fail_heat");
            Assert.AreEqual("cold", cold.Parameters["heat"]);
            Assert.AreEqual(2, cold.Parameters["n"]);
        }

        [Test]
        public void Heat_NotSentForOtherStageOrAfterWin()
        {
            var h = New().Launch();
            Attempt(h, 12, StageResult.Fail);
            h.Do(Fact.StageStart(11, false));
            Assert.IsFalse(h.Names.Contains("ftu_consecutive_fail_heat"));
            h.Do(Fact.StageEnd(StageResult.Abandon));
            Attempt(h, 12, StageResult.Win);        // its own start still reports heat (streak 1); the win then clears it
            h.Clear();
            h.Do(Fact.StageStart(12, false));
            Assert.IsFalse(h.Names.Contains("ftu_consecutive_fail_heat"));
        }

        [Test]
        public void WinStreak_Global_ResetByAnyFail()
        {
            var h = New().Launch();
            Attempt(h, 3, StageResult.Win);
            Attempt(h, 4, StageResult.Win);
            Assert.AreEqual("normal", h.Last("ftu_consecutive_win").Parameters["type"]);
            Attempt(h, 5, StageResult.Win);
            Attempt(h, 6, StageResult.Win);
            Attempt(h, 7, StageResult.Win);
            var e = h.Last("ftu_consecutive_win");
            Assert.AreEqual(5, e.Parameters["n"]);
            Assert.AreEqual("strong", e.Parameters["type"]);
            Attempt(h, 8, StageResult.Fail);
            h.Clear();
            Attempt(h, 8, StageResult.Win);
            Assert.IsFalse(h.Names.Contains("ftu_consecutive_win"));
        }

        [Test]
        public void Streak_CarriesAcrossFtuBoundary_PrefixFollowsCurrentSession()
        {
            var h = New().Launch();
            Attempt(h, 12, StageResult.Fail);
            h.Kill(5);
            Attempt(h, 12, StageResult.Fail);
            Assert.AreEqual(2, h.Last("consecutive_fail").Parameters["n"]);
        }
    }
}

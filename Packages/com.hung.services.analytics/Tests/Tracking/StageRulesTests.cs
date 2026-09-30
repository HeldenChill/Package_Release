using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class StageRulesTests
    {
        static TrackingHarness New() => new TrackingHarness(_ => new IRule[]
        {
            new StageLifecycleRule(), new StageCompleteRule(), new CheckpointRule()
        });

        [Test]
        public void Lifecycle_StartFailQuit_CarryStageWaveReplay()
        {
            var h = New().Launch();
            h.Do(Fact.StageStart(12, false));
            h.Do(Fact.WaveReached(5));
            h.Do(Fact.StageEnd(StageResult.Fail));
            h.Do(Fact.StageStart(12, true));
            h.Do(Fact.WaveReached(3));
            h.Do(Fact.StageEnd(StageResult.Abandon));

            Assert.AreEqual(12, h.Last("ftu_stage_start").Parameters["stage"]);
            var fail = h.Last("ftu_stage_fail");
            Assert.AreEqual(5, fail.Parameters["wave"]);
            Assert.AreEqual(0, fail.Parameters["replay"]);
            var quit = h.Last("ftu_stage_quit");
            Assert.AreEqual(3, quit.Parameters["wave"]);
            Assert.AreEqual(1, quit.Parameters["replay"]);
        }

        [Test]
        public void Complete_FirstClearSumsAllAttempts_ReplaySendsThisAttempt()
        {
            var h = New().Launch();
            h.Do(Fact.StageStart(12, false)); h.Play(4); h.Do(Fact.StageEnd(StageResult.Fail));
            h.Do(Fact.StageStart(12, false)); h.Play(3); h.Do(Fact.StageEnd(StageResult.Fail));
            h.Do(Fact.StageStart(12, false)); h.Play(5); h.Do(Fact.StageEnd(StageResult.Win));
            var first = h.Last("ftu_stage_complete");
            Assert.AreEqual(12, first.Parameters["time"]);
            Assert.AreEqual(0, first.Parameters["replay"]);

            h.Do(Fact.StageStart(12, true)); h.Play(2.5); h.Do(Fact.StageEnd(StageResult.Win));
            var replay = h.Last("ftu_stage_complete");
            Assert.AreEqual(2, replay.Parameters["time"]);
            Assert.AreEqual(1, replay.Parameters["replay"]);
        }

        [Test]
        public void Complete_TotalSurvivesKill_AndTicksOutsideStageDoNotCount()
        {
            var h = New().Launch();
            h.Do(Fact.StageStart(3, false)); h.Play(4); h.Do(Fact.StageEnd(StageResult.Fail));
            h.Play(30);
            h.Kill(10);
            h.Do(Fact.StageStart(3, false)); h.Play(3); h.Do(Fact.StageEnd(StageResult.Win));
            var e = h.Last("stage_complete");
            Assert.AreEqual(7, e.Parameters["time"]);
        }

        [Test]
        public void Checkpoint_KilledMidStage_FiresAtNextLaunch_WithOpenersPrefix()
        {
            var h = New().Launch();
            h.Do(Fact.StageStart(1, false));
            h.Do(Fact.WaveReached(4));
            h.Kill(5);
            var e = h.Last("ftu_checkpoint");
            Assert.AreEqual(1, e.Parameters["stage"]);
            Assert.AreEqual(4, e.Parameters["wave"]);
            h.Clear();
            h.Kill(5);
            Assert.IsFalse(h.Names.Contains("ftu_checkpoint"));
        }

        [Test]
        public void Checkpoint_StageEndedOrBackgroundReturn_DoesNotFire()
        {
            var h = New().Launch();
            h.Do(Fact.StageStart(1, false));
            h.Background(3);
            h.Do(Fact.StageEnd(StageResult.Fail));
            h.Kill(5);
            Assert.IsFalse(h.Names.Exists(n => n.EndsWith("checkpoint")));
        }
    }
}

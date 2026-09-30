using System;
using System.Linq;
using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class TrackingPipelineTests
    {
        [Test]
        public void FactsBeforeStart_AreQueued_ThenReplayedInOrder()
        {
            var spy = new SpyRule();
            var h = new TrackingHarness(_ => new IRule[] { spy });
            var ctx = new TrackingContext(h.Settings, h.Clock);
            var service = new AnalyticsService(() => 0f);
            var p = new TrackingPipeline(ctx, new EventEmitter(service, h.Settings), h.Store);
            p.Add(spy);
            p.Dispatch(Fact.ColdStart());
            p.Dispatch(Fact.StageStart(1, false));
            Assert.IsEmpty(spy.Seen);
            p.Start();
            CollectionAssert.AreEqual(new[] { FactKind.ColdStart, FactKind.StageStart }, spy.Seen);
        }

        [Test]
        public void ThrowingRule_IsIsolated_AndWarnedOnce()
        {
            var bad = new SpyRule { Throw = new InvalidOperationException("x") };
            var good = new SpyRule();
            var h = new TrackingHarness(_ => new IRule[] { bad, good }).Launch();
            h.Do(Fact.Blur());
            Assert.AreEqual(2, good.Seen.Count);
            Assert.AreEqual(1, h.Warnings.Count(w => w.Contains("SpyRule")));
        }

        [Test]
        public void FirstLaunchIsFtu_SecondIsNot_StatePersists()
        {
            var h = new TrackingHarness(_ => new IRule[0]).Launch();
            Assert.IsTrue(h.Ctx.IsFtu);
            h.Do(Fact.Progress(7));
            h.Kill(5);
            Assert.IsFalse(h.Ctx.IsFtu);
            Assert.AreEqual(7, h.Ctx.CurrentStage);
        }

        [Test]
        public void WaveOrEndWithoutStage_IsIgnored_WithWarning()
        {
            var spy = new SpyRule();
            var h = new TrackingHarness(_ => new IRule[] { spy }).Launch();
            h.Do(Fact.WaveReached(3));
            h.Do(Fact.StageEnd(StageResult.Win));
            CollectionAssert.AreEqual(new[] { FactKind.ColdStart }, spy.Seen);
            Assert.AreEqual(2, h.Warnings.Count);
        }

        [Test]
        public void StageStartWhileInStage_WarnsOnce_AndStartsFresh()
        {
            var h = new TrackingHarness(_ => new IRule[0]).Launch();
            h.Do(Fact.StageStart(1, false));
            h.Do(Fact.WaveReached(4));
            int attempt = h.Ctx.Attempt;
            h.Do(Fact.StageStart(2, false));
            h.Do(Fact.StageStart(3, false));
            Assert.AreEqual(3, h.Ctx.Stage);
            Assert.AreEqual(0, h.Ctx.Wave);
            Assert.AreEqual(attempt + 2, h.Ctx.Attempt);
            Assert.AreEqual(1, h.Warnings.Count);
        }

        [Test]
        public void SavesOnlyAtSavePoints()
        {
            var h = new TrackingHarness(_ => new IRule[0]).Launch();
            int afterBoot = h.Store.SaveCount;
            h.Do(Fact.StageStart(1, false));
            h.Play(1);
            Assert.AreEqual(afterBoot, h.Store.SaveCount);
            h.Do(Fact.StageEnd(StageResult.Fail));
            Assert.AreEqual(afterBoot + 1, h.Store.SaveCount);
        }

        [Test]
        public void SaveFailure_DoesNotThrow_AndRetriesAtNextSavePoint()
        {
            var h = new TrackingHarness(_ => new IRule[0]).Launch();
            h.Store.Available = false;
            h.Do(Fact.Progress(9));
            Assert.DoesNotThrow(() => h.Do(Fact.Blur()));
            Assert.IsTrue(h.Ctx.State.Dirty);
            h.Store.Available = true;
            h.Do(Fact.Focus());
            h.Do(Fact.Blur());
            Assert.IsFalse(h.Ctx.State.Dirty);
        }

        [Test]
        public void UnavailableStore_FtuIsOver_WithWarning()
        {
            var h = new TrackingHarness(_ => new IRule[0]);
            h.Store.Available = false;
            h.Launch();
            Assert.IsFalse(h.Ctx.IsFtu);
            Assert.IsTrue(h.Warnings.Any(w => w.Contains("unavailable")));
        }

        [Test]
        public void LoadFailure_NeverOverwritesRealSave_ForTheSession()
        {
            var h = new TrackingHarness(_ => new IRule[0]);
            var real = new TrackingStateModel();
            real.Values["ctx.current_stage"] = "8";
            h.Store.Save(real);
            int saves = h.Store.SaveCount;
            h.Store.Available = false;
            h.Launch();
            h.Store.Available = true;
            h.Do(Fact.Progress(1));
            h.Do(Fact.Blur());
            Assert.AreEqual(saves, h.Store.SaveCount);
            Assert.IsTrue(h.Store.TryLoad(out var kept));
            Assert.AreEqual("8", kept.Values["ctx.current_stage"]);
        }

        [Test]
        public void UnknownVersion_ResetsState_AndFtuIsOver()
        {
            var h = new TrackingHarness(_ => new IRule[0]);
            var future = new TrackingStateModel { Version = 99 };
            future.Values["ctx.current_stage"] = "5";
            h.Store.Save(future);
            h.Launch();
            Assert.IsFalse(h.Ctx.IsFtu);
            Assert.AreEqual(0, h.Ctx.CurrentStage);
            Assert.IsTrue(h.Warnings.Any(w => w.Contains("version")));
        }

        [Test]
        public void AwayMinutes_MeasuredOnFocusAndColdStart()
        {
            var h = new TrackingHarness(_ => new IRule[0]).Launch();
            h.Background(45);
            Assert.AreEqual(45, h.Ctx.AwayMinutes, 1e-6);
            h.Kill(120);
            Assert.AreEqual(120, h.Ctx.AwayMinutes, 1e-6);
        }
    }
}

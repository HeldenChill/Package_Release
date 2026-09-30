using System;
using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>consecutive_fail and consecutive_fail_wave, kept per stage and reset only by a win on that stage (E6, E7, E8).</summary>
    internal sealed class FailStreakRule : IRule
    {
        internal static string StreakKey(int stage) => "fail_streak." + stage;
        static string LastWaveKey(int stage) => "fail_wave." + stage + ".wave";
        static string RunKey(int stage) => "fail_wave." + stage + ".run";

        public void Declare(EventEmitter emit, TrackingSettings s)
        {
            emit.Declare("consecutive_fail", "consecutive_fail_stage_{stage}_{type}", (long)s.maxStage * 3);
            emit.Declare("consecutive_fail_wave", "consecutive_fail_stage_{stage}_wave_{wave}", (long)s.maxStage * s.maxWave);
        }

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.StageEnd) return;
            var s = ctx.State;
            int stage = ctx.Stage;
            if (fact.Result == StageResult.Win)
            {
                new Counter(s, StreakKey(stage)).Reset();
                new Counter(s, RunKey(stage)).Reset();
                s.Remove(LastWaveKey(stage));
                return;
            }
            if (fact.Result != StageResult.Fail) return;

            int n = new Counter(s, StreakKey(stage)).Inc();
            if (n >= 2)
                emit.Emit("consecutive_fail", ctx.IsFtu, new Dictionary<string, object>
                {
                    { "stage", stage }, { "n", n }, { "type", ctx.Settings.failType.Label(n) }
                });

            var run = new Counter(s, RunKey(stage));
            if (s.GetLong(LastWaveKey(stage), -1) != ctx.Wave)
            {
                run.Reset();
                s.SetLong(LastWaveKey(stage), ctx.Wave);
            }
            int r = run.Inc();
            if (r >= 2)
                emit.Emit("consecutive_fail_wave", ctx.IsFtu, new Dictionary<string, object>
                {
                    { "stage", stage }, { "wave", ctx.Wave }, { "n", r }
                });
        }
    }

    /// <summary>consecutive_fail_heat: wall-clock gap from a fail to the next start of that stage (E9).</summary>
    internal sealed class FailHeatRule : IRule
    {
        static string MarkKey(int stage) => "fail_heat." + stage;

        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("consecutive_fail_heat", "consecutive_fail_stage_{stage}_heat_{heat}", (long)s.maxStage * 4);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind == FactKind.StageEnd)
            {
                var mark = new Since(ctx.State, MarkKey(ctx.Stage));
                if (fact.Result == StageResult.Fail) mark.Mark(ctx.Now);
                else if (fact.Result == StageResult.Win) mark.Clear();
                return;
            }
            if (fact.Kind != FactKind.StageStart) return;
            var since = new Since(ctx.State, MarkKey(ctx.Stage));
            int n = new Counter(ctx.State, FailStreakRule.StreakKey(ctx.Stage)).Value;
            if (n < 1 || !since.HasMark) return;
            double gap = since.Minutes(ctx.Now);
            since.Clear();
            emit.Emit("consecutive_fail_heat", ctx.IsFtu, new Dictionary<string, object>
            {
                { "stage", ctx.Stage }, { "n", n }, { "heat", ctx.Settings.heat.Label(gap) }, { "gap_min", (int)Math.Floor(gap) }
            });
        }
    }

    /// <summary>consecutive_win across all stages, reset by any fail (F5, E16).</summary>
    internal sealed class WinStreakRule : IRule
    {
        const string Key = "win_streak";

        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("consecutive_win", "consecutive_win_{type}", 2);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.StageEnd) return;
            var streak = new Counter(ctx.State, Key);
            if (fact.Result == StageResult.Fail) { streak.Reset(); return; }
            if (fact.Result != StageResult.Win) return;
            int n = streak.Inc();
            if (n >= 2)
                emit.Emit("consecutive_win", ctx.IsFtu, new Dictionary<string, object>
                {
                    { "stage", ctx.Stage }, { "n", n }, { "type", ctx.Settings.winType.Label(n) }
                });
        }
    }
}

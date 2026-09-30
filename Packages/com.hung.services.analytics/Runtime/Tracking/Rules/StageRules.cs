using System;
using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>stage_start, stage_fail and stage_quit (X8, X10, D3).</summary>
    internal sealed class StageLifecycleRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings s)
        {
            emit.Declare("stage_start", "stage_{stage}_start", s.maxStage);
            emit.Declare("stage_fail", "stage_{stage}_fail_wave_{wave}", (long)s.maxStage * s.maxWave);
            emit.Declare("stage_quit", "stage_{stage}_quit_wave_{wave}", (long)s.maxStage * s.maxWave);
        }

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind == FactKind.StageStart)
            {
                emit.Emit("stage_start", ctx.IsFtu, new Dictionary<string, object> { { "stage", ctx.Stage }, { "replay", ctx.Replay ? 1 : 0 } });
                return;
            }
            if (fact.Kind != FactKind.StageEnd || fact.Result == StageResult.Win) return;
            string name = fact.Result == StageResult.Fail ? "stage_fail" : "stage_quit";
            emit.Emit(name, ctx.IsFtu, new Dictionary<string, object>
            {
                { "stage", ctx.Stage }, { "wave", ctx.Wave }, { "replay", ctx.Replay ? 1 : 0 }
            });
        }
    }

    /// <summary>
    /// stage_complete {stage, time, replay}. For the first clear, time is the foreground minutes summed over every attempt (E3).
    /// Once cleared, time is this attempt's minutes (D1).
    /// </summary>
    internal sealed class StageCompleteRule : IRule
    {
        double _attemptMinutes;

        static string TotalKey(int stage) => "stage_time." + stage;
        static string ClearedKey(int stage) => "stage_cleared." + stage;

        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("stage_complete", "stage_{stage}_complete", s.maxStage);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            switch (fact.Kind)
            {
                case FactKind.StageStart:
                    _attemptMinutes = 0;
                    break;
                case FactKind.Tick when ctx.InStage:
                    _attemptMinutes += fact.Minutes;
                    if (!ctx.State.Has(ClearedKey(ctx.Stage))) new Accum(ctx.State, TotalKey(ctx.Stage)).Add(fact.Minutes);
                    break;
                case FactKind.StageEnd when fact.Result == StageResult.Win:
                    var total = new Accum(ctx.State, TotalKey(ctx.Stage));
                    bool firstClear = !ctx.State.Has(ClearedKey(ctx.Stage));
                    int time = (int)Math.Floor(firstClear ? total.Value : _attemptMinutes);
                    if (firstClear)
                    {
                        ctx.State.SetLong(ClearedKey(ctx.Stage), 1);
                        total.Reset();
                    }
                    emit.Emit("stage_complete", ctx.IsFtu, new Dictionary<string, object>
                    {
                        { "stage", ctx.Stage }, { "time", time }, { "replay", ctx.Replay ? 1 : 0 }
                    });
                    break;
            }
        }
    }

    /// <summary>checkpoint {stage, wave}: a stage left open when the process died, reported at the next cold start (E12, F1).</summary>
    internal sealed class CheckpointRule : IRule
    {
        const string Key = "checkpoint";

        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("checkpoint", "checkpoint_stage_{stage}_wave_{wave}", (long)s.maxStage * s.maxWave);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            var pending = new Pending(ctx.State, Key);
            switch (fact.Kind)
            {
                case FactKind.ColdStart:
                    if (pending.TryTake(out var r))
                        emit.Emit("checkpoint", r.Ftu, new Dictionary<string, object> { { "stage", r.GetInt("stage") }, { "wave", r.GetInt("wave") } });
                    break;
                case FactKind.StageStart:
                    pending.Open(ctx.IsFtu, ("stage", ctx.Stage), ("wave", 0));
                    break;
                case FactKind.WaveReached:
                    if (pending.IsOpen) pending.Set("wave", fact.Wave);
                    break;
                case FactKind.StageEnd:
                    pending.Discard();
                    break;
            }
        }
    }
}

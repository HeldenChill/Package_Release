using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>gacha {pool, count, type, cost_type, cost_amount}; type comes from the wall-clock gap since the last pull of the same pool (E10).</summary>
    internal sealed class GachaRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("gacha", "gacha_{pool}_{type}", 16);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.Gacha) return;
            var since = new Since(ctx.State, "gacha." + fact.Pool);
            string type = since.HasMark ? ctx.Settings.gachaType.Label(since.Minutes(ctx.Now)) : "first";
            since.Mark(ctx.Now);
            emit.Emit("gacha", ctx.IsFtu, new Dictionary<string, object>
            {
                { "pool", fact.Pool }, { "count", fact.Count }, { "type", type },
                { "cost_type", fact.CostType }, { "cost_amount", fact.CostAmount }
            });
        }
    }

    /// <summary>gacha_feeling: the first qualifying action after a pull decides positive, normal or negative (E11).</summary>
    internal sealed class GachaFeelingRule : IRule
    {
        const string Key = "gacha_feeling";

        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("gacha_feeling", "gacha_feeling_{type}", 3);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            var pending = new Pending(ctx.State, Key);
            switch (fact.Kind)
            {
                case FactKind.Gacha:
                    Resolve(pending, "normal", emit);
                    pending.Open(ctx.IsFtu, ("pool", fact.Pool), ("count", fact.Count));
                    break;
                case FactKind.Monetize:
                    Resolve(pending, "positive", emit);
                    break;
                case FactKind.StageStart:
                    Resolve(pending, "normal", emit);
                    break;
                case FactKind.Feature when ctx.Settings.feelingNormalFeatures.Contains(fact.Id):
                    Resolve(pending, "normal", emit);
                    break;
                case FactKind.Focus:
                case FactKind.ColdStart:
                    if (ctx.AwayMinutes > ctx.Settings.feelingNegativeMinutes) Resolve(pending, "negative", emit);
                    break;
            }
        }

        static void Resolve(Pending pending, string type, EventEmitter emit)
        {
            if (!pending.TryTake(out var r)) return;
            emit.Emit("gacha_feeling", r.Ftu, new Dictionary<string, object>
            {
                { "pool", r.Get("pool") }, { "count", r.GetInt("count") }, { "type", type }
            });
        }
    }
}

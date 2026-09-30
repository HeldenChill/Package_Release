using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>
    /// Turns <c>Feature(id)</c> into an event named <c>id</c>, for ids listed in <see cref="TrackingSettings.featureEvents"/>.
    /// While in a stage it adds stage and replay; explicit args win (e.g. milestone_claim at Home).
    /// </summary>
    internal sealed class FeatureRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings s)
        {
            foreach (string id in s.featureEvents) emit.Declare(id, id + "_stage_{stage}", s.maxStage);
        }

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.Feature || !ctx.Settings.featureEvents.Contains(fact.Id)) return;
            var args = new Dictionary<string, object>();
            if (fact.Args != null)
                foreach (var kv in fact.Args) args[kv.Key] = kv.Value;
            if (ctx.InStage)
            {
                if (!args.ContainsKey("stage")) args["stage"] = ctx.Stage;
                if (!args.ContainsKey("replay")) args["replay"] = ctx.Replay ? 1 : 0;
            }
            emit.Emit(fact.Id, ctx.IsFtu, args);
        }
    }

    /// <summary>tutorial {id, step, ftu, skipped}. Never prefixed; fires in every session (T1-T6).</summary>
    internal sealed class TutorialRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings s) =>
            emit.Declare("tutorial", "tutorial_{id}_{step}", 100);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.Tutorial) return;
            var args = new Dictionary<string, object>
            {
                { "id", fact.Id }, { "step", fact.Phase.ToString().ToLowerInvariant() }
            };
            if (fact.Skipped) args["skipped"] = 1;
            emit.EmitTagged("tutorial", ctx.IsFtu, args);
        }
    }

    /// <summary>The package's opt-in standard rule pack, in dispatch order (spec section 7.1).</summary>
    public static class StandardRules
    {
        /// <summary>New instances of every standard rule. User properties are installed separately and always.</summary>
        public static IReadOnlyList<IRule> Create() => new IRule[]
        {
            new OpenAppRule(), new LoginDayRule(), new FtuTimeplayRule(),
            new StageLifecycleRule(), new StageCompleteRule(), new CheckpointRule(),
            new FailStreakRule(), new FailHeatRule(), new WinStreakRule(),
            new GachaRule(), new GachaFeelingRule(),
            new FeatureRule(), new TutorialRule()
        };
    }
}

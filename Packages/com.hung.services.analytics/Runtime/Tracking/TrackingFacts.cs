using System;
using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>The game-facing fact API (spec section 3). Every call becomes one <see cref="Fact"/>.</summary>
    public sealed class TrackingFacts
    {
        internal static readonly TrackingFacts NoOp = new TrackingFacts(null);
        readonly Action<Fact> _dispatch;

        internal TrackingFacts(Action<Fact> dispatch) { _dispatch = dispatch; }

        /// <summary>A stage attempt started. <paramref name="replay"/> is true when the stage was cleared before.</summary>
        public void StageStart(int stage, bool replay) => Raise(Fact.StageStart(stage, replay));
        /// <summary>A wave started in the current stage.</summary>
        public void WaveReached(int wave) => Raise(Fact.WaveReached(wave));
        /// <summary>The current stage attempt ended.</summary>
        public void StageEnd(StageResult result) => Raise(Fact.StageEnd(result));
        /// <summary>The player's next uncleared stage changed; sets user property current_stage.</summary>
        public void Progress(int currentStage) => Raise(Fact.Progress(currentStage));
        /// <summary>A gacha pull.</summary>
        public void Gacha(string pool, int count, string costType, long costAmount) => Raise(Fact.Gacha(pool, count, costType, costAmount));
        /// <summary>A tutorial flow reached <paramref name="phase"/>.</summary>
        public void Tutorial(string flowId, TutorialPhase phase, bool skipped = false) => Raise(Fact.Tutorial(flowId, phase, skipped));

        /// <summary>The player used feature <paramref name="id"/>; the event is sent only when the id is in the settings' featureEvents.</summary>
        public void Feature(string id, params (string key, object value)[] args)
        {
            Dictionary<string, object> map = null;
            if (args != null && args.Length > 0)
            {
                map = new Dictionary<string, object>(args.Length);
                foreach (var (key, value) in args) map[key] = value;
            }
            Raise(Fact.Feature(id, map));
        }

        internal void Raise(in Fact fact) => _dispatch?.Invoke(fact);
    }

    /// <summary>Static entry point: <see cref="Facts"/> for the game, <see cref="Register"/> for game-specific rules.</summary>
    public static class AnalyticsTracking
    {
        static readonly List<IRule> PendingRules = new List<IRule>();
        static TrackingPipeline _pipeline;
        static ITrackingNamingProfile _namingProfile;

        /// <summary>Sets the optional wire naming profile; null restores canonical output.</summary>
        public static void SetNamingProfile(ITrackingNamingProfile profile)
        {
            _namingProfile = profile;
            _pipeline?.SetNamingProfile(profile);
        }

        /// <summary>The fact API; a no-op until tracking is installed, so never null.</summary>
        public static TrackingFacts Facts { get; private set; } = TrackingFacts.NoOp;

        /// <summary>Adds a game rule. Rules registered before install are added right after the standard pack.</summary>
        public static void Register(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (_pipeline != null) _pipeline.Add(rule);
            else PendingRules.Add(rule);
        }

        internal static void Install(TrackingPipeline pipeline)
        {
            _pipeline = pipeline;
            pipeline?.SetNamingProfile(_namingProfile);
            if (pipeline == null)
            {
                Facts = TrackingFacts.NoOp;
                return;
            }
            foreach (var rule in PendingRules) pipeline.Add(rule);
            PendingRules.Clear();
            Facts = new TrackingFacts(f => pipeline.Dispatch(f));
        }

        internal static void Reset()
        {
            _pipeline = null;
            _namingProfile = null;
            PendingRules.Clear();
            Facts = TrackingFacts.NoOp;
        }
    }
}

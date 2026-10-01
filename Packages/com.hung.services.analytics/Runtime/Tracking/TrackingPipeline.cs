using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Analytics.Tracking
{
    /// <summary>
    /// Runs facts through the context and the rules (spec section 2). Facts dispatched before <see cref="Start"/> are queued and replayed,
    /// because persistence may not be configured at BeforeSceneLoad. A throwing rule is logged once and never stops the others.
    /// State is saved at ColdStart, Blur and StageEnd.
    /// </summary>
    public sealed class TrackingPipeline
    {
        readonly TrackingContext _ctx;
        readonly EventEmitter _emit;
        ITrackingStateStore _store;
        readonly Action<string> _warn;
        readonly List<IRule> _rules = new List<IRule>();
        readonly List<Fact> _queue = new List<Fact>();
        readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>Creates a pipeline; call <see cref="Start"/> once persistence is ready.</summary>
        public TrackingPipeline(TrackingContext ctx, EventEmitter emit, ITrackingStateStore store, Action<string> warn = null)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _emit = emit ?? throw new ArgumentNullException(nameof(emit));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _warn = warn ?? Debug.LogWarning;
        }

        /// <summary>Whether state is loaded and facts run immediately.</summary>
        public bool Started { get; private set; }
        /// <summary>The context rules read.</summary>
        public TrackingContext Context => _ctx;

        /// <summary>Sets the optional wire naming profile; null restores canonical output.</summary>
        public void SetNamingProfile(ITrackingNamingProfile profile) =>
            _emit.SetNamingProfile(profile);

        /// <summary>Adds a rule after the existing ones and runs its declarations.</summary>
        public void Add(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules.Add(rule);
            rule.Declare(_emit, _ctx.Settings);
        }

        /// <summary>Loads state, then replays queued facts in order. Later calls do nothing.</summary>
        public void Start()
        {
            if (Started) return;
            bool available = _store.TryLoad(out var model);
            if (!available)
            {
                Warn("store", "[Analytics] Tracking state store unavailable; state is in memory for this session and FTU is treated as over.");
                // Never write the empty session state over a save we failed to read (spec section 4 fallback).
                _store = new InMemoryTrackingStateStore();
            }
            if (_ctx.Load(model, available)) Warn("version", "[Analytics] Tracking state has an unknown version; reset to empty and FTU is treated as over.");
            Started = true;
            var queued = _queue.ToArray();
            _queue.Clear();
            foreach (var f in queued) Run(f);
        }

        /// <summary>Runs <paramref name="fact"/> now, or queues it before <see cref="Start"/>.</summary>
        public void Dispatch(in Fact fact)
        {
            if (!Started) { _queue.Add(fact); return; }
            Run(fact);
        }

        void Run(Fact f)
        {
            if (!Accept(f)) return;
            _ctx.Before(f);
            foreach (var rule in _rules)
            {
                try { rule.OnFact(f, _ctx, _emit); }
                catch (Exception e) { Warn("rule." + rule.GetType().FullName, $"[Analytics] Rule {rule.GetType().Name} threw; other rules still ran: {e.Message}"); }
            }
            _ctx.After(f);
            if (IsSavePoint(f.Kind) && _ctx.State.Dirty && _store.Save(_ctx.State.Model)) _ctx.State.ClearDirty();
        }

        bool Accept(in Fact f)
        {
            if ((f.Kind == FactKind.WaveReached || f.Kind == FactKind.StageEnd) && !_ctx.InStage)
            {
                Warn("order." + f.Kind, $"[Analytics] {f.Kind} with no stage in progress; ignored.");
                return false;
            }
            if (f.Kind == FactKind.StageStart && _ctx.InStage)
                Warn("order.StageStart", "[Analytics] StageStart while a stage is in progress; treated as a new attempt.");
            return true;
        }

        static bool IsSavePoint(FactKind k) => k == FactKind.ColdStart || k == FactKind.Blur || k == FactKind.StageEnd;

        void Warn(string key, string message)
        {
            if (_warned.Add(key)) _warn(message);
        }
    }
}

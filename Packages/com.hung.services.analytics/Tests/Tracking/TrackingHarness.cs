using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Analytics.Tracking;
using Hung.Base;

namespace Hung.Analytics.Tests.Tracking
{
    internal sealed class FakeClock : ITrackingClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc); // a Monday
        public DateTime LocalToday => UtcNow.Date;
        public void Advance(double minutes) => UtcNow = UtcNow.AddMinutes(minutes);
    }

    /// <summary>
    /// Drives a real pipeline with a fake clock, an in-memory store and a recording backend. <see cref="Launch"/> simulates a new
    /// app process: fresh context and rules over the same store, then ColdStart.
    /// </summary>
    internal sealed class TrackingHarness
    {
        readonly Func<TrackingSettings, IEnumerable<IRule>> _rules;

        public TrackingHarness(Func<TrackingSettings, IEnumerable<IRule>> rules, TrackingSettings settings = null)
        {
            _rules = rules;
            Settings = settings != null ? settings : TrackingSettings.CreateDefault();
        }

        public InMemoryTrackingStateStore Store { get; } = new InMemoryTrackingStateStore();
        public FakeClock Clock { get; } = new FakeClock();
        public RecordingAnalyticsBackend Backend { get; } = new RecordingAnalyticsBackend();
        public List<string> Warnings { get; } = new List<string>();
        public TrackingSettings Settings { get; }
        public TrackingPipeline Pipeline { get; private set; }
        public TrackingContext Ctx => Pipeline.Context;

        public TrackingHarness Launch()
        {
            var service = new AnalyticsService(() => 0f);
            service.AddBackend(Backend, AnalyticsCategory.All);
            var ctx = new TrackingContext(Settings, Clock);
            Pipeline = new TrackingPipeline(ctx, new EventEmitter(service, Settings, Warnings.Add), Store, Warnings.Add);
            foreach (var rule in _rules(Settings)) Pipeline.Add(rule);
            Pipeline.Dispatch(Fact.ColdStart());
            Pipeline.Start();
            return this;
        }

        public void Do(Fact fact) => Pipeline.Dispatch(fact);

        /// <summary>Plays <paramref name="minutes"/> of foreground time and advances the clock.</summary>
        public void Play(double minutes)
        {
            Pipeline.Dispatch(Fact.Tick(minutes));
            Clock.Advance(minutes);
        }

        public void Background(double minutes)
        {
            Do(Fact.Blur());
            Clock.Advance(minutes);
            Do(Fact.Focus());
        }

        /// <summary>Backgrounds (the save point), the OS kills the app, and it relaunches after <paramref name="minutes"/>.</summary>
        public void Kill(double minutes)
        {
            Do(Fact.Blur());
            Clock.Advance(minutes);
            Launch();
        }

        public List<string> Names => Backend.Events.Select(e => e.Name).ToList();
        public RecordedEvent Last(string name) => Backend.Events.Last(e => e.Name == name);
        public void Clear() => Backend.Events.Clear();
    }

    internal sealed class SpyRule : IRule
    {
        public readonly List<FactKind> Seen = new List<FactKind>();
        public Exception Throw;
        public void Declare(EventEmitter emit, TrackingSettings settings) { }
        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            Seen.Add(fact.Kind);
            if (Throw != null) throw Throw;
        }
    }
}

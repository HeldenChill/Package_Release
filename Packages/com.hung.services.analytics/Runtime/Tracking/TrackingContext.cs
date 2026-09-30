using System;
using System.Globalization;

namespace Hung.Analytics.Tracking
{
    /// <summary>
    /// Package-owned tracking context (spec section 4). Updated before rules run for a fact (<c>Before</c>) and after them (<c>After</c>),
    /// so a StageEnd rule still sees the stage and wave.
    /// </summary>
    public sealed class TrackingContext
    {
        const string ColdStartsKey = "ctx.cold_starts";
        const string InstallDayKey = "ctx.install_day";
        const string LastSeenKey = "ctx.last_seen_utc";
        const string CurrentStageKey = "ctx.current_stage";
        const string AttemptKey = "ctx.attempt";

        /// <summary>Creates an unloaded context; the pipeline loads it on Start.</summary>
        public TrackingContext(TrackingSettings settings, ITrackingClock clock)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            State = new TrackingState(null);
        }

        /// <summary>Active settings.</summary>
        public TrackingSettings Settings { get; }
        /// <summary>Wall clock.</summary>
        public ITrackingClock Clock { get; }
        /// <summary>Persisted state that operators wrap.</summary>
        public TrackingState State { get; private set; }
        /// <summary>Current UTC time.</summary>
        public DateTime Now => Clock.UtcNow;
        /// <summary>Today's local date as yyyy-MM-dd.</summary>
        public string Today => Clock.LocalToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        /// <summary>True during the first app process lifetime (X6).</summary>
        public bool IsFtu { get; private set; }
        /// <summary>Between StageStart and StageEnd.</summary>
        public bool InStage { get; private set; }
        /// <summary>Current stage; valid while <see cref="InStage"/>.</summary>
        public int Stage { get; private set; }
        /// <summary>Current wave; 0 before the first WaveReached.</summary>
        public int Wave { get; private set; }
        /// <summary>Replay flag of the current attempt.</summary>
        public bool Replay { get; private set; }
        /// <summary>Persisted attempt counter, incremented per StageStart.</summary>
        public int Attempt => (int)State.GetLong(AttemptKey);
        /// <summary>False between Blur and Focus.</summary>
        public bool Focused { get; private set; } = true;
        /// <summary>Minutes since the last Blur, measured at Focus and ColdStart; 0 when unknown.</summary>
        public double AwayMinutes { get; private set; }
        /// <summary>Player's next uncleared stage, from Progress.</summary>
        public int CurrentStage => (int)State.GetLong(CurrentStageKey);
        /// <summary>Local calendar days since install; the install day is 1.</summary>
        public int DaysSinceInstall =>
            (int)(Clock.LocalToday - new DateTime(State.GetLong(InstallDayKey, Clock.LocalToday.Ticks))).TotalDays + 1;

        /// <summary>
        /// Loads <paramref name="model"/>. An unavailable store or an unknown version gives empty state with FTU treated as over.
        /// Returns true when the version was reset.
        /// </summary>
        internal bool Load(TrackingStateModel model, bool available)
        {
            bool versionReset = model != null && model.Version != TrackingStateModel.CurrentVersion;
            State = new TrackingState(versionReset ? null : model);
            // ponytail: seeding 1 makes the next ColdStart count 2, so a returning user is never re-counted as FTU.
            if (!available || versionReset) State.SetLong(ColdStartsKey, 1);
            return versionReset;
        }

        internal void Before(in Fact f)
        {
            switch (f.Kind)
            {
                case FactKind.ColdStart:
                    long n = State.GetLong(ColdStartsKey) + 1;
                    State.SetLong(ColdStartsKey, n);
                    IsFtu = n == 1;
                    if (!State.Has(InstallDayKey)) State.SetLong(InstallDayKey, Clock.LocalToday.Ticks);
                    AwayMinutes = MinutesSinceLastSeen();
                    Focused = true;
                    break;
                case FactKind.Focus:
                    AwayMinutes = MinutesSinceLastSeen();
                    Focused = true;
                    break;
                case FactKind.Blur:
                    Focused = false;
                    break;
                case FactKind.StageStart:
                    InStage = true;
                    Stage = f.Stage;
                    Replay = f.Replay;
                    Wave = 0;
                    State.SetLong(AttemptKey, Attempt + 1);
                    break;
                case FactKind.WaveReached:
                    Wave = f.Wave;
                    break;
                case FactKind.Progress:
                    State.SetLong(CurrentStageKey, f.CurrentStage);
                    break;
            }
        }

        internal void After(in Fact f)
        {
            if (f.Kind == FactKind.StageEnd) InStage = false;
            else if (f.Kind == FactKind.Blur) State.SetLong(LastSeenKey, Now.Ticks);
        }

        double MinutesSinceLastSeen() =>
            State.Has(LastSeenKey) ? Math.Max(0, (Now - new DateTime(State.GetLong(LastSeenKey), DateTimeKind.Utc)).TotalMinutes) : 0;
    }
}

using System.Collections.Generic;
using System.Globalization;

namespace Hung.Analytics.Tracking
{
    /// <summary>Keeps user properties ftu, days_since_install and current_stage current (X7, E4).</summary>
    internal sealed class UserPropertiesRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings settings) { }

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind == FactKind.ColdStart)
            {
                emit.SetUserProperty("ftu", ctx.IsFtu ? "1" : "0");
                emit.SetUserProperty("days_since_install", ctx.DaysSinceInstall.ToString(CultureInfo.InvariantCulture));
                emit.SetUserProperty("current_stage", ctx.CurrentStage.ToString(CultureInfo.InvariantCulture));
            }
            else if (fact.Kind == FactKind.Progress)
                emit.SetUserProperty("current_stage", fact.CurrentStage.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>open_app on every cold start; the first is ftu_open_app (E1).</summary>
    internal sealed class OpenAppRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings settings) => emit.Declare("open_app", null, 0);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind == FactKind.ColdStart) emit.Emit("open_app", ctx.IsFtu, null);
        }
    }

    /// <summary>login_day {n} once per local calendar day, after FTU; n is uncapped, so always A mode (E4, D2).</summary>
    internal sealed class LoginDayRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings settings) => emit.Declare("login_day", null, 0);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.ColdStart || ctx.IsFtu) return;
            if (!new OncePer(ctx.State, "login_day").First(ctx.Today)) return;
            emit.Emit("login_day", false, new Dictionary<string, object> { { "n", ctx.DaysSinceInstall } });
        }
    }

    /// <summary>ftu_timeplay {min} when FTU foreground time crosses each mark (F4).</summary>
    internal sealed class FtuTimeplayRule : IRule
    {
        public void Declare(EventEmitter emit, TrackingSettings settings) =>
            emit.Declare("timeplay", "timeplay_{min}", settings.ftuTimeplayMarks.Count);

        public void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit)
        {
            if (fact.Kind != FactKind.Tick || !ctx.IsFtu) return;
            var acc = new Accum(ctx.State, "ftu_timeplay");
            double before = acc.Value;
            double after = acc.Add(fact.Minutes);
            foreach (int mark in ctx.Settings.ftuTimeplayMarks)
                if (Accum.Crossed(before, after, mark))
                    emit.Emit("timeplay", true, new Dictionary<string, object> { { "min", mark } });
        }
    }
}

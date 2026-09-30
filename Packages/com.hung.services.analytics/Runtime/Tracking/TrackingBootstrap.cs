using Hung.Base;

namespace Hung.Analytics.Tracking
{
    /// <summary>Builds the tracking pipeline and installs it into <see cref="AnalyticsTracking"/>.</summary>
    public static class TrackingBootstrap
    {
        /// <summary>
        /// Builds the pipeline with the user-property rule and, when enabled, the standard pack plus any registered rules. It installs
        /// the pipeline and queues ColdStart. The caller calls <see cref="TrackingPipeline.Start"/> once persistence is ready.
        /// Returns null, with a no-op facade, when <c>settings.enabled</c> is off.
        /// </summary>
        public static TrackingPipeline Install(IAnalyticsService sink, TrackingSettings settings, ITrackingStateStore store, ITrackingClock clock)
        {
            if (settings == null) settings = TrackingSettings.CreateDefault();
            if (!settings.enabled)
            {
                AnalyticsTracking.Install(null);
                return null;
            }
            var ctx = new TrackingContext(settings, clock);
            var pipeline = new TrackingPipeline(ctx, new EventEmitter(sink, settings), store);
            pipeline.Add(new UserPropertiesRule());
            if (settings.useStandardRules)
                foreach (var rule in StandardRules.Create()) pipeline.Add(rule);
            AnalyticsTracking.Install(pipeline);
            pipeline.Dispatch(Fact.ColdStart());
            return pipeline;
        }
    }
}

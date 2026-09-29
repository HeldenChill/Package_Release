using System;

namespace Hung.Base
{
    /// <summary>
    /// Routing tag for an analytics event. Each analytics backend subscribes to a set of categories
    /// and only receives events whose category intersects that set.
    /// Values are serialized in the analytics settings asset: never renumber.
    /// </summary>
    [Flags]
    public enum AnalyticsCategory
    {
        /// <summary>Routed to no backend.</summary>
        None = 0,
        /// <summary>Ad funnel: request, load, show, click, finish, first ad session.</summary>
        Ads = 1,
        /// <summary>Business-critical: IAP purchases, level pass.</summary>
        Product = 2,
        /// <summary>Game design: tutorial, currency, level start/fail/stars, custom GD events.</summary>
        Design = 4,
        /// <summary>Ad impression revenue from <see cref="IRevenueEventSink"/>.</summary>
        Revenue = 8,
        /// <summary>Every category.</summary>
        All = Ads | Product | Design | Revenue,
    }
}

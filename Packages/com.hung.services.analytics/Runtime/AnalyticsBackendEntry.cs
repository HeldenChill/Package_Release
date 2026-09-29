using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.Analytics
{
    /// <summary>Per-backend row in <c>AnalyticsSettings</c>: switch, routed categories, SDK keys.</summary>
    [Serializable]
    public sealed class AnalyticsBackendEntry
    {
        /// <summary>Backend id, one of <see cref="AnalyticsBackendIds"/>.</summary>
        public string id;
        /// <summary>Editor switch; Apply in the settings window turns it into a scripting define.</summary>
        public bool enabled;
        /// <summary>Event categories this backend receives.</summary>
        public AnalyticsCategory categories;
        /// <summary>AppsFlyer dev key or AppMetrica API key. Unused by Firebase and GameAnalytics.</summary>
        [Tooltip("AppsFlyer dev key / AppMetrica API key")]
        public string apiKey;
        /// <summary>AppsFlyer iOS app id (numeric). Unused by other backends.</summary>
        [Tooltip("AppsFlyer iOS app id")]
        public string appId;

        /// <summary>Required by Unity serialization.</summary>
        public AnalyticsBackendEntry() { }

        /// <summary>Creates an entry routing <paramref name="categories"/> to backend <paramref name="id"/>.</summary>
        public AnalyticsBackendEntry(string id, AnalyticsCategory categories)
        {
            this.id = id;
            this.categories = categories;
        }

        /// <summary>
        /// Default routing (spec §5.2). AppMetrica excludes Revenue: its plugin auto-tracks MAX and
        /// IronSource revenue already, so routing Revenue too would double-count.
        /// </summary>
        public static AnalyticsBackendEntry CreateDefault(string id) => id switch
        {
            AnalyticsBackendIds.Firebase => new AnalyticsBackendEntry(id, AnalyticsCategory.All),
            AnalyticsBackendIds.AppsFlyer => new AnalyticsBackendEntry(id, AnalyticsCategory.Product | AnalyticsCategory.Revenue),
            AnalyticsBackendIds.AppMetrica => new AnalyticsBackendEntry(id, AnalyticsCategory.Product),
            AnalyticsBackendIds.GameAnalytics => new AnalyticsBackendEntry(id, AnalyticsCategory.Ads | AnalyticsCategory.Product | AnalyticsCategory.Design),
            _ => new AnalyticsBackendEntry(id, AnalyticsCategory.All),
        };

        /// <summary>One default entry per known backend id.</summary>
        public static List<AnalyticsBackendEntry> CreateDefaults()
        {
            var list = new List<AnalyticsBackendEntry>();
            foreach (var id in AnalyticsBackendIds.All) list.Add(CreateDefault(id));
            return list;
        }
    }
}

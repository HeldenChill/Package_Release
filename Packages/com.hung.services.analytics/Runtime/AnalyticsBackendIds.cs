using System;
using System.Collections.Generic;

namespace Hung.Analytics
{
    /// <summary>Known backend ids and the scripting defines that compile their integration assemblies.</summary>
    public static class AnalyticsBackendIds
    {
        /// <summary>Firebase Analytics (+ Remote Config, Messaging).</summary>
        public const string Firebase = "firebase";
        /// <summary>AppsFlyer attribution.</summary>
        public const string AppsFlyer = "appsflyer";
        /// <summary>AppMetrica analytics.</summary>
        public const string AppMetrica = "appmetrica";
        /// <summary>GameAnalytics.</summary>
        public const string GameAnalytics = "gameanalytics";
        /// <summary>Every known id, in settings-window order.</summary>
        public static readonly string[] All = { Firebase, AppsFlyer, AppMetrica, GameAnalytics };
        /// <summary>Prefix shared by every analytics define.</summary>
        public const string DefinePrefix = "HUNG_ANALYTICS_";

        /// <summary>Scripting define for <paramref name="id"/>, e.g. <c>HUNG_ANALYTICS_FIREBASE</c>.</summary>
        public static string Define(string id) => DefinePrefix + id.ToUpperInvariant();

        /// <summary>
        /// Returns <paramref name="current"/> with every <c>HUNG_ANALYTICS_*</c> define removed and one define
        /// per enabled id appended. Other defines keep their order; blanks are dropped.
        /// </summary>
        public static string[] ApplyDefines(IEnumerable<string> current, IEnumerable<string> enabledIds)
        {
            var result = new List<string>();
            foreach (var define in current)
            {
                if (string.IsNullOrWhiteSpace(define)) continue;
                var trimmed = define.Trim();
                if (!trimmed.StartsWith(DefinePrefix, StringComparison.Ordinal)) result.Add(trimmed);
            }
            foreach (var id in enabledIds)
            {
                var define = Define(id);
                if (!result.Contains(define)) result.Add(define);
            }
            return result.ToArray();
        }
    }
}

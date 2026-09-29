using System.Collections.Generic;

namespace Hung.Analytics
{
    /// <summary>
    /// One analytics SDK adapter. Implementations live in define-gated integration assemblies and
    /// register themselves with <see cref="AnalyticsBackends"/>. The proxy never calls an SDK directly.
    /// </summary>
    public interface IAnalyticsBackend
    {
        /// <summary>Stable id, one of <see cref="AnalyticsBackendIds"/>. Matches the settings entry.</summary>
        string Id { get; }

        /// <summary>Starts the SDK. Throwing excludes this backend for the session.</summary>
        void Initialize(AnalyticsBackendEntry entry, bool debugLog);

        /// <summary>Sends one named event. <paramref name="parameters"/> may be null.</summary>
        void LogEvent(string name, IReadOnlyDictionary<string, object> parameters);

        /// <summary>Sets one user-level property.</summary>
        void SetUserProperty(string key, string value);

        /// <summary>
        /// Sends one ad impression revenue record. <paramref name="extra"/> uses neutral keys:
        /// mediation, country, ad_unit, ad_type, placement.
        /// </summary>
        void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra);
    }
}

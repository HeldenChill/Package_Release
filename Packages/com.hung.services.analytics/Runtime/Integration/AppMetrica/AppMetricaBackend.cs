using System;
using System.Collections.Generic;
using Io.AppMetrica;
using Io.AppMetrica.Profile;
using UnityEngine;
using ProfileAttribute = Io.AppMetrica.Profile.Attribute;

namespace Hung.Analytics.Backends
{
    /// <summary>
    /// AppMetrica backend. Activates with the settings apiKey. Revenue is off by default: the plugin's own
    /// MAX/IronSource auto adapters already report it; LogAdRevenue exists for mediations they do not cover.
    /// </summary>
    public sealed class AppMetricaBackend : IAnalyticsBackend
    {
        /// <inheritdoc/>
        public string Id => AnalyticsBackendIds.AppMetrica;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register() => AnalyticsBackends.Register(new AppMetricaBackend());

        /// <inheritdoc/>
        public void Initialize(AnalyticsBackendEntry entry, bool debugLog)
        {
            if (string.IsNullOrEmpty(entry.apiKey))
                throw new InvalidOperationException("AppMetrica API key (apiKey) is empty in HungAnalyticsSettings.");
            AppMetrica.Activate(new AppMetricaConfig(entry.apiKey) { Logs = debugLog });
        }

        /// <inheritdoc/>
        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            string json = AnalyticsText.ToJson(parameters);
            if (json == null) AppMetrica.ReportEvent(name);
            else AppMetrica.ReportEvent(name, json);
        }

        /// <inheritdoc/>
        public void SetUserProperty(string key, string value) =>
            AppMetrica.ReportUserProfile(new UserProfile().Apply(ProfileAttribute.CustomString(key).WithValue(value ?? "")));

        /// <inheritdoc/>
        public void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra)
        {
            var revenue = new AdRevenue(value, string.IsNullOrEmpty(currency) ? "USD" : currency)
            {
                AdNetwork = source,
                AdUnitId = Extra(extra, "ad_unit"),
                AdPlacementName = Extra(extra, "placement"),
                Payload = extra == null ? null : ToDictionary(extra),
            };
            AppMetrica.ReportAdRevenue(revenue);
        }

        static string Extra(IReadOnlyDictionary<string, string> extra, string key) =>
            extra != null && extra.TryGetValue(key, out var v) ? v : null;

        static IDictionary<string, string> ToDictionary(IReadOnlyDictionary<string, string> extra)
        {
            var copy = new Dictionary<string, string>();
            foreach (var kv in extra) copy[kv.Key] = kv.Value;
            return copy;
        }
    }
}

using System.Collections.Generic;
using GameAnalyticsSDK;
using UnityEngine;

namespace Hung.Analytics.Backends
{
    /// <summary>
    /// GameAnalytics backend: every event becomes a design event. Keys live in GA's own Settings asset.
    /// Ensures a persistent GameAnalytics object exists before initializing.
    /// </summary>
    public sealed class GameAnalyticsBackend : IAnalyticsBackend
    {
        // GA design event id parts are limited to 64 chars; Sanitize keeps to a charset GA accepts.
        const int MaxEventIdLength = 64;

        /// <inheritdoc/>
        public string Id => AnalyticsBackendIds.GameAnalytics;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register() => AnalyticsBackends.Register(new GameAnalyticsBackend());

        /// <inheritdoc/>
        public void Initialize(AnalyticsBackendEntry entry, bool debugLog)
        {
            if (Object.FindObjectOfType<GameAnalytics>() == null)
            {
                var host = new GameObject("GameAnalytics");
                host.AddComponent<GameAnalytics>();
                Object.DontDestroyOnLoad(host);
            }
            GameAnalytics.Initialize();
        }

        /// <inheritdoc/>
        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            string eventId = AnalyticsText.Sanitize(name, MaxEventIdLength);
            if (parameters == null || parameters.Count == 0)
            {
                GameAnalytics.NewDesignEvent(eventId);
                return;
            }
            var fields = new Dictionary<string, object>(parameters.Count);
            foreach (var kv in parameters) fields[kv.Key] = kv.Value;
            GameAnalytics.NewDesignEvent(eventId, fields);
        }

        /// <inheritdoc/>
        // ponytail: GA custom dimensions must be pre-declared in the GA dashboard; no-op until GD defines them.
        public void SetUserProperty(string key, string value) { }

        /// <inheritdoc/>
        // ponytail: GA ad-revenue API is mediation-specific; Revenue is not in GA's default categories. Add when GD asks.
        public void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra) { }
    }
}

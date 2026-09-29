using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

namespace Hung.Analytics.Backends
{
    /// <summary>
    /// AppsFlyer backend. Initializes the SDK from the settings entry (apiKey = dev key, appId = iOS app id),
    /// replacing the vendor AppsFlyerObject prefab. Maps neutral revenue keys to AppsFlyer's AdRevenueScheme.
    /// </summary>
    public sealed class AppsFlyerBackend : IAnalyticsBackend
    {
        static readonly Dictionary<string, string> RevenueSchemeKeys = new Dictionary<string, string>
        {
            { "country", AdRevenueScheme.COUNTRY },
            { "ad_unit", AdRevenueScheme.AD_UNIT },
            { "ad_type", AdRevenueScheme.AD_TYPE },
            { "placement", AdRevenueScheme.PLACEMENT },
        };

        readonly Dictionary<string, string> _userProperties = new Dictionary<string, string>();

        /// <inheritdoc/>
        public string Id => AnalyticsBackendIds.AppsFlyer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register() => AnalyticsBackends.Register(new AppsFlyerBackend());

        /// <inheritdoc/>
        public void Initialize(AnalyticsBackendEntry entry, bool debugLog)
        {
            if (string.IsNullOrEmpty(entry.apiKey))
                throw new InvalidOperationException("AppsFlyer dev key (apiKey) is empty in HungAnalyticsSettings.");
            AppsFlyer.setIsDebug(debugLog);
            AppsFlyer.initSDK(entry.apiKey, entry.appId ?? "");
            AppsFlyer.startSDK();
        }

        /// <inheritdoc/>
        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters) =>
            AppsFlyer.sendEvent(name, AnalyticsText.ToStringDictionary(parameters) ?? new Dictionary<string, string>());

        /// <inheritdoc/>
        public void SetUserProperty(string key, string value)
        {
            // setAdditionalData replaces the whole custom-data map, so resend every property.
            _userProperties[key] = value ?? "";
            AppsFlyer.setAdditionalData(new Dictionary<string, string>(_userProperties));
        }

        /// <inheritdoc/>
        public void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra)
        {
            string mediation = extra != null && extra.TryGetValue("mediation", out var m) ? m : null;
            var data = new AFAdRevenueData(string.IsNullOrEmpty(source) ? "unknown" : source, MapMediation(mediation),
                string.IsNullOrEmpty(currency) ? "USD" : currency, value);

            Dictionary<string, string> additional = null;
            if (extra != null)
            {
                additional = new Dictionary<string, string>();
                foreach (var kv in extra)
                {
                    if (kv.Key == "mediation") continue;
                    additional[RevenueSchemeKeys.TryGetValue(kv.Key, out var scheme) ? scheme : kv.Key] = kv.Value;
                }
            }
            AppsFlyer.logAdRevenue(data, additional);
        }

        static MediationNetwork MapMediation(string mediation) => mediation switch
        {
            "ironsource" => MediationNetwork.IronSource,
            "max" => MediationNetwork.ApplovinMax,
            "admob" => MediationNetwork.GoogleAdMob,
            _ => MediationNetwork.Custom,
        };
    }
}

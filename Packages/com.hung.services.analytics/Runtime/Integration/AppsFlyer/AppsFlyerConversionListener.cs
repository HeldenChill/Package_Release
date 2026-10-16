using System;
using System.Collections.Generic;
using System.Text;
using AppsFlyerSDK;
using UnityEngine;

namespace Hung.Analytics.Backends
{
    /// <summary>
    /// Receives AppsFlyer conversion callbacks for this launch and logs an allow-listed summary (af_status, media_source,
    /// campaign, is_first_launch) when debug logging or a development build is on. It sends nothing and never logs device
    /// identifiers. Live attribution tests read the "[AppsFlyer] Conversion" line from the device log. AppsFlyer finds this
    /// object by its GameObject name.
    /// </summary>
    public sealed class AppsFlyerConversionListener : MonoBehaviour, IAppsFlyerConversionData
    {
        static readonly string[] Keys = { "af_status", "media_source", "campaign", "is_first_launch" };
        bool _log;

        internal static AppsFlyerConversionListener Create(bool debugLog)
        {
            var go = new GameObject(nameof(AppsFlyerConversionListener));
            DontDestroyOnLoad(go);
            var listener = go.AddComponent<AppsFlyerConversionListener>();
            listener._log = debugLog || Debug.isDebugBuild;
            return listener;
        }

        public void onConversionDataSuccess(string conversionData)
        {
            if (_log) Debug.Log("[AppsFlyer] Conversion " + Summary(conversionData));
        }

        public void onConversionDataFail(string error) => Debug.LogWarning("[AppsFlyer] Conversion data failed: " + error);

        public void onAppOpenAttribution(string attributionData)
        {
            if (_log) Debug.Log("[AppsFlyer] App open attribution " + Summary(attributionData));
        }

        public void onAppOpenAttributionFailure(string error) => Debug.LogWarning("[AppsFlyer] App open attribution failed: " + error);

        /// <summary>Only the allow-listed keys, as key=value pairs; "-" for a missing key.</summary>
        public static string Summary(string json)
        {
            Dictionary<string, object> data = null;
            try { data = AppsFlyer.CallbackStringToDictionary(json); }
            catch (Exception) { }
            if (data == null) return "(unreadable)";
            var sb = new StringBuilder();
            foreach (var key in Keys)
                sb.Append(key).Append('=').Append(data.TryGetValue(key, out var value) && value != null ? value.ToString() : "-").Append(' ');
            return sb.ToString().TrimEnd();
        }
    }
}

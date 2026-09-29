using System;
using System.Collections.Generic;
using System.Globalization;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.Messaging;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Hung.Analytics.Backends
{
    /// <summary>
    /// Firebase Analytics backend. Events sent before the async dependency check finishes are queued
    /// (cap 100) and flushed on the main thread once Firebase is ready. Also starts Remote Config
    /// (<see cref="FirebaseRemoteConfigCache"/>) and Messaging, and requests Android notification permission.
    /// </summary>
    public sealed class FirebaseBackend : IAnalyticsBackend
    {
        const int MaxPending = 100;
        const int MaxNameLength = 40;
        const int MaxParamValueLength = 100;
        const int MaxUserPropertyNameLength = 24;
        const int MaxUserPropertyValueLength = 36;

        // LevelPlay raises impression (revenue) callbacks on a background thread, so state below is guarded by _gate.
        readonly object _gate = new object();
        readonly Queue<Action> _pending = new Queue<Action>();
        bool _ready;
        bool _failed;
        bool _debugLog;

        /// <inheritdoc/>
        public string Id => AnalyticsBackendIds.Firebase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register() => AnalyticsBackends.Register(new FirebaseBackend());

        /// <inheritdoc/>
        public void Initialize(AnalyticsBackendEntry entry, bool debugLog)
        {
            _debugLog = debugLog;
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
                Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
#endif
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
                {
                    lock (_gate)
                    {
                        _failed = true;
                        _pending.Clear();
                    }
                    string reason = task.IsFaulted ? task.Exception?.GetBaseException().Message : task.IsCanceled ? "canceled" : task.Result.ToString();
                    Debug.LogError($"[Analytics] Firebase unavailable ({reason}); its events are dropped this session.");
                    return;
                }

                // Flush before starting Messaging/Remote Config so a throw there cannot lose queued events.
                lock (_gate)
                {
                    _ready = true;
                    bool reported = false;
                    while (_pending.Count > 0)
                    {
                        var action = _pending.Dequeue();
                        try { action(); }
                        catch (Exception e)
                        {
                            if (!reported) Debug.LogError($"[Analytics] Firebase queued event failed: {e.Message}. Further queued failures are silenced.");
                            reported = true;
                        }
                    }
                }
                FirebaseMessaging.TokenReceived += OnTokenReceived;
                FirebaseMessaging.MessageReceived += OnMessageReceived;
                Application.quitting += Shutdown;
                FirebaseRemoteConfigCache.Fetch();
                if (_debugLog) Debug.Log("[Analytics] Firebase ready, pending events flushed.");
            });
        }

        /// <inheritdoc/>
        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            string eventName = AnalyticsText.Sanitize(name, MaxNameLength);
            var snapshot = parameters == null ? null : new List<KeyValuePair<string, object>>(parameters);
            Run(() => FirebaseAnalytics.LogEvent(eventName, ToParameters(snapshot)));
        }

        /// <inheritdoc/>
        public void SetUserProperty(string key, string value)
        {
            string name = AnalyticsText.Sanitize(key, MaxUserPropertyNameLength);
            string text = Truncate(value ?? "", MaxUserPropertyValueLength);
            Run(() => FirebaseAnalytics.SetUserProperty(name, text));
        }

        /// <inheritdoc/>
        public void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra)
        {
            var parameters = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("ad_platform", Extra(extra, "mediation")),
                new KeyValuePair<string, object>("ad_source", source ?? ""),
                new KeyValuePair<string, object>("ad_format", Extra(extra, "ad_type")),
                new KeyValuePair<string, object>("ad_unit_name", Extra(extra, "ad_unit")),
                new KeyValuePair<string, object>("currency", string.IsNullOrEmpty(currency) ? "USD" : currency),
                new KeyValuePair<string, object>("value", value),
            };
            Run(() => FirebaseAnalytics.LogEvent("ad_impression", ToParameters(parameters)));
        }

        void Run(Action action)
        {
            lock (_gate)
            {
                if (!_ready && !_failed)
                {
                    if (_pending.Count < MaxPending) _pending.Enqueue(action);
                    else if (_debugLog) Debug.LogWarning("[Analytics] Firebase not ready and pending queue is full; event dropped.");
                    return;
                }
                if (_failed) return;
            }
            action();
        }

        static Parameter[] ToParameters(List<KeyValuePair<string, object>> parameters)
        {
            if (parameters == null || parameters.Count == 0) return Array.Empty<Parameter>();
            var result = new Parameter[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                string key = AnalyticsText.Sanitize(parameters[i].Key, MaxNameLength);
                object value = parameters[i].Value;
                result[i] = value switch
                {
                    byte or sbyte or short or ushort or int or uint or long => new Parameter(key, Convert.ToInt64(value, CultureInfo.InvariantCulture)),
                    float or double or decimal => new Parameter(key, Convert.ToDouble(value, CultureInfo.InvariantCulture)),
                    bool b => new Parameter(key, b ? 1L : 0L),
                    _ => new Parameter(key, Truncate(AnalyticsText.ToInvariant(value), MaxParamValueLength)),
                };
            }
            return result;
        }

        static string Extra(IReadOnlyDictionary<string, string> extra, string key) =>
            extra != null && extra.TryGetValue(key, out var v) && v != null ? v : "";

        static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

        void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            if (_debugLog) Debug.Log("[Analytics] Firebase Messaging token received.");
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (_debugLog) Debug.Log($"[Analytics] Firebase message {e.Message.MessageId} from {e.Message.From}.");
        }

        void Shutdown()
        {
            FirebaseMessaging.TokenReceived -= OnTokenReceived;
            FirebaseMessaging.MessageReceived -= OnMessageReceived;
            Application.quitting -= Shutdown;
        }
    }
}

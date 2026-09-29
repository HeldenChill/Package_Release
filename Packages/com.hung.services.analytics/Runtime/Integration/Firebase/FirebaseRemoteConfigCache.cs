using System;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

namespace Hung.Analytics.Backends
{
    /// <summary>
    /// Non-blocking Firebase Remote Config reads. Fetch + activate start when <see cref="FirebaseBackend"/> is ready
    /// (minimum fetch interval 0 in debug builds, 12 h otherwise). Every TryGet returns false until then, or when
    /// the key has no remote value, so callers keep their own defaults. No Locator slot yet: add an
    /// IRemoteConfig contract when the first consumer needs one.
    /// </summary>
    public static class FirebaseRemoteConfigCache
    {
        /// <summary>True after a successful fetch + activate.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>Raised on the main thread after each successful fetch + activate.</summary>
        public static event Action Fetched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            IsReady = false;
            Fetched = null;
        }

        internal static void Fetch()
        {
            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            var minInterval = Debug.isDebugBuild ? TimeSpan.Zero : TimeSpan.FromHours(12);
            remoteConfig.FetchAsync(minInterval).ContinueWithOnMainThread(fetch =>
            {
                if (fetch.IsFaulted || fetch.IsCanceled)
                {
                    Debug.LogWarning($"[Analytics] Remote Config fetch failed ({fetch.Exception?.GetBaseException().Message ?? "canceled"}); callers keep defaults.");
                    return;
                }
                remoteConfig.ActivateAsync().ContinueWithOnMainThread(activate =>
                {
                    if (activate.IsFaulted || activate.IsCanceled)
                    {
                        Debug.LogWarning("[Analytics] Remote Config activate failed; callers keep defaults.");
                        return;
                    }
                    IsReady = true;
                    Fetched?.Invoke();
                });
            });
        }

        /// <summary>Remote string for <paramref name="key"/>; false if not ready or not set remotely.</summary>
        public static bool TryGetString(string key, out string value)
        {
            value = null;
            if (!TryGet(key, out var config)) return false;
            value = config.StringValue;
            return true;
        }

        /// <summary>Remote long for <paramref name="key"/>; false if not ready or not set remotely.</summary>
        public static bool TryGetLong(string key, out long value)
        {
            value = 0;
            if (!TryGet(key, out var config)) return false;
            value = config.LongValue;
            return true;
        }

        /// <summary>Remote double for <paramref name="key"/>; false if not ready or not set remotely.</summary>
        public static bool TryGetDouble(string key, out double value)
        {
            value = 0;
            if (!TryGet(key, out var config)) return false;
            value = config.DoubleValue;
            return true;
        }

        /// <summary>Remote bool for <paramref name="key"/>; false if not ready or not set remotely.</summary>
        public static bool TryGetBool(string key, out bool value)
        {
            value = false;
            if (!TryGet(key, out var config)) return false;
            value = config.BooleanValue;
            return true;
        }

        static bool TryGet(string key, out ConfigValue value)
        {
            value = default;
            if (!IsReady || string.IsNullOrEmpty(key)) return false;
            value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            return value.Source == ValueSource.RemoteValue;
        }
    }
}

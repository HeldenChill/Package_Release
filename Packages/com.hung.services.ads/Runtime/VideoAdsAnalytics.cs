using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.Ads
{
    /// <summary>
    /// Publisher ad-lifecycle events video_ads_available, video_ads_started and video_ads_watch (Azur prototype
    /// analytics contract). Sent as <see cref="AnalyticsCategory.Product"/> so they reach product analytics
    /// (AppMetrica) and not Ads-only routes. Legacy ad events are unchanged.
    /// </summary>
    public static class VideoAdsAnalytics
    {
        /// <summary>ad_type of a rewarded ad.</summary>
        public const string Rewarded = "rewarded";
        /// <summary>ad_type of an interstitial ad.</summary>
        public const string Interstitial = "interstitial";
        /// <summary>Seconds a repeated rewarded not_available for one placement is dropped (contract: 60).</summary>
        public const float UnavailableRepeatSeconds = 60f;

        /// <summary>Receives every event. Default logs to Locator.Analytics as Product. Test seam.</summary>
        public static Action<string, IReadOnlyDictionary<string, object>> Sink = DefaultSink;
        /// <summary>True when the device has a network connection. Test seam.</summary>
        public static Func<bool> HasConnection = DefaultHasConnection;
        /// <summary>Seconds clock for the unavailable suppression. Test seam.</summary>
        public static Func<float> Clock = DefaultClock;

        static readonly Dictionary<Placement, float> LastRewardedUnavailable = new Dictionary<Placement, float>();

        /// <summary>Restores the defaults and clears suppression. Domain reload is off, so this runs every play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetForPlay()
        {
            Sink = DefaultSink;
            HasConnection = DefaultHasConnection;
            Clock = DefaultClock;
            LastRewardedUnavailable.Clear();
        }

        /// <summary>
        /// A show was requested. A rewarded not_available for the same placement within 60 s of the previous one is
        /// dropped; a success clears that placement. Interstitial is never suppressed.
        /// </summary>
        public static void Available(string adType, Placement placement, bool available)
        {
            if (available)
            {
                if (adType == Rewarded) LastRewardedUnavailable.Remove(placement);
                Send("video_ads_available", adType, placement, "success");
                return;
            }
            if (adType == Rewarded)
            {
                float now = Clock();
                if (LastRewardedUnavailable.TryGetValue(placement, out float last) && now - last < UnavailableRepeatSeconds) return;
                LastRewardedUnavailable[placement] = now;
            }
            Send("video_ads_available", adType, placement, "not_available");
        }

        /// <summary>The ad was handed to the provider for display.</summary>
        public static void Started(string adType, Placement placement) =>
            Send("video_ads_started", adType, placement, "success");

        /// <summary>A started display ended with <paramref name="result"/>.</summary>
        public static void Watch(string adType, Placement placement, AdsShowResult result) =>
            Send("video_ads_watch", adType, placement, WatchResult(result));

        /// <summary>watched for a completed display, canceled when closed early, failed otherwise.</summary>
        public static string WatchResult(AdsShowResult result)
        {
            switch (result.Outcome)
            {
                case AdsRequestOutcome.Completed: return "watched";
                case AdsRequestOutcome.Skipped: return "canceled";
                default: return "failed";
            }
        }

        static void Send(string name, string adType, Placement placement, string resultValue)
        {
            var args = new Dictionary<string, object>
            {
                { "ad_type", adType },
                { "placement", placement.ToString().ToLowerInvariant() },
                { "result", resultValue },
                { "connection", HasConnection() },
            };
            try { Sink?.Invoke(name, args); }
            catch (Exception e) { Debug.LogWarning("[Ads] " + name + " analytics failed: " + e.Message); }
        }

        static void DefaultSink(string name, IReadOnlyDictionary<string, object> args) =>
            Locator.Analytics?.LogEvent(name, AnalyticsCategory.Product, args);

        static bool DefaultHasConnection() => Application.internetReachability != NetworkReachability.NotReachable;

        static float DefaultClock() => Time.realtimeSinceStartup;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Analytics.Tracking
{
    /// <summary>Wire format: A = fixed name + params, B = variables inside the name (X1).</summary>
    public enum OutputMode { A, B }

    /// <summary>Per-event output mode override.</summary>
    [Serializable]
    public sealed class ModeOverride
    {
        /// <summary>Logical event name (no ftu_ prefix).</summary>
        public string eventName;
        /// <summary>Mode for that event.</summary>
        public OutputMode mode;
    }

    /// <summary>
    /// Tracking configuration, loaded from <c>Resources/HungTrackingSettings.asset</c>. Every field has a code default,
    /// so the asset is optional (ADR-E5-0007).
    /// </summary>
    [CreateAssetMenu(menuName = "Hung/Analytics Tracking Settings", fileName = ResourcePath)]
    public sealed class TrackingSettings : ScriptableObject
    {
        /// <summary>Resources path (no extension).</summary>
        public const string ResourcePath = "HungTrackingSettings";

        /// <summary>Master switch; off installs a no-op <c>AnalyticsTracking.Facts</c>.</summary>
        public bool enabled = true;
        /// <summary>Register the package's standard rule pack.</summary>
        public bool useStandardRules = true;
        /// <summary>Mode for events without an override.</summary>
        public OutputMode defaultMode = OutputMode.A;
        /// <summary>Per-event mode overrides.</summary>
        public List<ModeOverride> modeOverrides = new List<ModeOverride>();
        /// <summary>Maximum distinct B-mode names, counting ftu_ twins. Headroom under Firebase's 500.</summary>
        public int bNameBudget = 400;
        /// <summary>Worst-case stage count, for B-name estimates.</summary>
        public int maxStage = 100;
        /// <summary>Worst-case wave count, for B-name estimates.</summary>
        public int maxWave = 20;
        /// <summary>consecutive_fail type (E6).</summary>
        public BucketSet failType = new BucketSet(new Bucket(2, "normal"), new Bucket(3, "rage"), new Bucket(1e9, "frustrated"));
        /// <summary>consecutive_win type (F5).</summary>
        public BucketSet winType = new BucketSet(new Bucket(4, "normal"), new Bucket(1e9, "strong"));
        /// <summary>Retry heat by minutes (E9).</summary>
        public BucketSet heat = new BucketSet(new Bucket(2, "burn"), new Bucket(10, "hot"), new Bucket(60, "chill"), new Bucket(1e9, "cold"));
        /// <summary>Gacha gap type by minutes (E10). ponytail: inclusive bounds, so exactly 5.0 min reads normal, not shy.</summary>
        public BucketSet gachaType = new BucketSet(new Bucket(1, "rush"), new Bucket(5, "normal"), new Bucket(1e9, "shy"));
        /// <summary>FTU foreground-minute marks (F4).</summary>
        public List<int> ftuTimeplayMarks = new List<int> { 5, 10, 15, 30 };
        /// <summary>Away time that resolves a pending gacha feeling as negative (E11).</summary>
        public double feelingNegativeMinutes = 30;
        /// <summary>Feature ids that resolve a pending gacha feeling as normal (E11).</summary>
        public List<string> feelingNormalFeatures = new List<string> { "pet_upgrade" };
        /// <summary>Feature ids that <c>Feature</c> facts turn into events of the same name.</summary>
        public List<string> featureEvents = new List<string>();

        /// <summary>Mode for <paramref name="eventName"/>.</summary>
        public OutputMode ModeFor(string eventName)
        {
            if (modeOverrides != null)
                foreach (var o in modeOverrides)
                    if (o != null && o.eventName == eventName) return o.mode;
            return defaultMode;
        }

        /// <summary>A fresh instance with code defaults.</summary>
        public static TrackingSettings CreateDefault() => CreateInstance<TrackingSettings>();
    }
}

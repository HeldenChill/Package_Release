using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Analytics.Tracking
{
    /// <summary>Loads and saves the tracking save model.</summary>
    public interface ITrackingStateStore
    {
        /// <summary>False when the store is unavailable; <paramref name="model"/> is then null.</summary>
        bool TryLoad(out TrackingStateModel model);
        /// <summary>False when the save did not happen.</summary>
        bool Save(TrackingStateModel model);
    }

    /// <summary>Keeps a copy in memory. Used as the fallback and in tests.</summary>
    public sealed class InMemoryTrackingStateStore : ITrackingStateStore
    {
        TrackingStateModel _saved;

        /// <summary>When false, every call reports unavailable.</summary>
        public bool Available { get; set; } = true;
        /// <summary>Successful saves so far.</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc/>
        public bool TryLoad(out TrackingStateModel model)
        {
            model = Available ? Copy(_saved) ?? new TrackingStateModel() : null;
            return Available;
        }

        /// <inheritdoc/>
        public bool Save(TrackingStateModel model)
        {
            if (!Available) return false;
            _saved = Copy(model);
            SaveCount++;
            return true;
        }

        static TrackingStateModel Copy(TrackingStateModel m) =>
            m == null ? null : new TrackingStateModel { Version = m.Version, Values = new Dictionary<string, string>(m.Values) };
    }

    /// <summary>
    /// Saves through the static <c>Database</c> facade in Hung.Base (com.hung.data persistence), like other game data.
    /// Any exception, including "not configured" when com.hung.data is absent, reports unavailable.
    /// </summary>
    public sealed class DatabaseTrackingStateStore : ITrackingStateStore
    {
        /// <summary>The persistence key.</summary>
        public const string SaveKey = "analytics-tracking";

        /// <inheritdoc/>
        public bool TryLoad(out TrackingStateModel model)
        {
            try
            {
                model = Database.Load<TrackingStateModel>(SaveKey) ?? new TrackingStateModel();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Tracking state load failed: {e.Message}");
                model = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Save(TrackingStateModel model)
        {
            try
            {
                Database.Save(model, SaveKey);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Tracking state save failed: {e.Message}");
                return false;
            }
        }
    }
}

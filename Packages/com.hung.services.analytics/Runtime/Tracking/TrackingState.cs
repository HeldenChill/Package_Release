using System;
using System.Collections.Generic;
using System.Globalization;

namespace Hung.Analytics.Tracking
{
    /// <summary>Persisted tracking state: the save model under key <c>analytics-tracking</c> (ADR-E5-0007).</summary>
    public sealed class TrackingStateModel
    {
        /// <summary>The schema version this build writes.</summary>
        public const int CurrentVersion = 1;
        /// <summary>The schema version of this instance.</summary>
        public int Version = CurrentVersion;
        /// <summary>Flat keyed values, invariant-culture strings.</summary>
        public Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    /// <summary>Typed, dirty-tracked access to a <see cref="TrackingStateModel"/>. Operators wrap keys of it.</summary>
    public sealed class TrackingState
    {
        readonly Dictionary<string, string> _values;

        /// <summary>Wraps <paramref name="model"/>; null means an empty model.</summary>
        public TrackingState(TrackingStateModel model)
        {
            Model = model ?? new TrackingStateModel();
            if (Model.Values == null) Model.Values = new Dictionary<string, string>();
            _values = Model.Values;
        }

        /// <summary>The underlying save model.</summary>
        public TrackingStateModel Model { get; }
        /// <summary>True when a value changed since the last <see cref="ClearDirty"/>.</summary>
        public bool Dirty { get; private set; }
        /// <summary>Marks the state as saved.</summary>
        public void ClearDirty() => Dirty = false;

        /// <summary>Whether <paramref name="key"/> exists.</summary>
        public bool Has(string key) => _values.ContainsKey(key);

        /// <summary>The raw string, or <paramref name="fallback"/>.</summary>
        public string GetString(string key, string fallback = null) =>
            _values.TryGetValue(key, out var v) ? v : fallback;

        /// <summary>Sets a raw string; marks dirty only on change.</summary>
        public void SetString(string key, string value)
        {
            if (_values.TryGetValue(key, out var old) && old == value) return;
            _values[key] = value;
            Dirty = true;
        }

        /// <summary>A long value, or <paramref name="fallback"/>.</summary>
        public long GetLong(string key, long fallback = 0) =>
            _values.TryGetValue(key, out var v) && long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : fallback;

        /// <summary>Sets a long value.</summary>
        public void SetLong(string key, long value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));

        /// <summary>A double value, or <paramref name="fallback"/>.</summary>
        public double GetDouble(string key, double fallback = 0) =>
            _values.TryGetValue(key, out var v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : fallback;

        /// <summary>Sets a double value (round-trip format).</summary>
        public void SetDouble(string key, double value) => SetString(key, value.ToString("R", CultureInfo.InvariantCulture));

        /// <summary>Removes <paramref name="key"/> if present.</summary>
        public void Remove(string key)
        {
            if (_values.Remove(key)) Dirty = true;
        }

        /// <summary>Removes every key starting with <paramref name="prefix"/>.</summary>
        public void RemovePrefix(string prefix)
        {
            var doomed = new List<string>();
            foreach (var key in _values.Keys)
                if (key.StartsWith(prefix, StringComparison.Ordinal)) doomed.Add(key);
            foreach (var key in doomed) Remove(key);
        }
    }
}

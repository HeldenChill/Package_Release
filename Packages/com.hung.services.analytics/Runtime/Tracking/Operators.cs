using System;
using System.Collections.Generic;
using System.Globalization;

namespace Hung.Analytics.Tracking
{
    /// <summary>A persisted counter under one state key.</summary>
    public readonly struct Counter
    {
        readonly TrackingState _s;
        readonly string _k;
        /// <summary>Wraps <paramref name="key"/> in <paramref name="state"/>.</summary>
        public Counter(TrackingState state, string key) { _s = state; _k = key; }
        /// <summary>The current count; 0 when unset.</summary>
        public int Value => (int)_s.GetLong(_k);
        /// <summary>Adds one and returns the new count.</summary>
        public int Inc() { int n = Value + 1; _s.SetLong(_k, n); return n; }
        /// <summary>Back to 0.</summary>
        public void Reset() => _s.Remove(_k);
    }

    /// <summary>Time since a persisted mark.</summary>
    public readonly struct Since
    {
        readonly TrackingState _s;
        readonly string _k;
        /// <summary>Wraps <paramref name="key"/> in <paramref name="state"/>.</summary>
        public Since(TrackingState state, string key) { _s = state; _k = key; }
        /// <summary>Whether a mark exists.</summary>
        public bool HasMark => _s.Has(_k);
        /// <summary>Stores <paramref name="utc"/> as the mark.</summary>
        public void Mark(DateTime utc) => _s.SetLong(_k, utc.Ticks);
        /// <summary>Minutes from the mark to <paramref name="utc"/>; 0 without a mark or when the clock went backwards.</summary>
        public double Minutes(DateTime utc) =>
            HasMark ? Math.Max(0, (utc - new DateTime(_s.GetLong(_k), DateTimeKind.Utc)).TotalMinutes) : 0;
        /// <summary>Removes the mark.</summary>
        public void Clear() => _s.Remove(_k);
    }

    /// <summary>A persisted running sum.</summary>
    public readonly struct Accum
    {
        readonly TrackingState _s;
        readonly string _k;
        /// <summary>Wraps <paramref name="key"/> in <paramref name="state"/>.</summary>
        public Accum(TrackingState state, string key) { _s = state; _k = key; }
        /// <summary>The current sum.</summary>
        public double Value => _s.GetDouble(_k);
        /// <summary>Adds <paramref name="amount"/> and returns the new sum.</summary>
        public double Add(double amount) { double v = Value + amount; _s.SetDouble(_k, v); return v; }
        /// <summary>Back to 0.</summary>
        public void Reset() => _s.Remove(_k);
        /// <summary>True when the sum went from below <paramref name="threshold"/> to at or above it.</summary>
        public static bool Crossed(double before, double after, double threshold) => before < threshold && after >= threshold;
    }

    /// <summary>A resolved <see cref="Pending"/> record.</summary>
    public readonly struct PendingRecord
    {
        readonly IReadOnlyDictionary<string, string> _fields;
        /// <summary>Creates a record.</summary>
        public PendingRecord(bool ftu, IReadOnlyDictionary<string, string> fields) { Ftu = ftu; _fields = fields; }
        /// <summary>Whether the session that opened the record was FTU; this decides the prefix.</summary>
        public bool Ftu { get; }
        /// <summary>A field's raw value, or null.</summary>
        public string Get(string field) => _fields != null && _fields.TryGetValue(field, out var v) ? v : null;
        /// <summary>A field as an int; 0 when missing.</summary>
        public int GetInt(string field) =>
            int.TryParse(Get(field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    /// <summary>
    /// A persisted record opened by one fact and resolved by a later one, possibly in a later session.
    /// Keys: <c>{key}.open</c>, <c>{key}.ftu</c>, <c>{key}.f.{field}</c>.
    /// </summary>
    public readonly struct Pending
    {
        readonly TrackingState _s;
        readonly string _k;
        /// <summary>Wraps <paramref name="key"/> in <paramref name="state"/>.</summary>
        public Pending(TrackingState state, string key) { _s = state; _k = key; }
        /// <summary>Whether a record is open.</summary>
        public bool IsOpen => _s.Has(_k + ".open");

        /// <summary>Opens a new record, replacing any open one.</summary>
        public void Open(bool ftu, params (string field, object value)[] fields)
        {
            Discard();
            _s.SetLong(_k + ".open", 1);
            _s.SetLong(_k + ".ftu", ftu ? 1 : 0);
            foreach (var (field, value) in fields) Set(field, value);
        }

        /// <summary>Sets one field of the open record.</summary>
        public void Set(string field, object value) => _s.SetString(_k + ".f." + field, AnalyticsText.ToInvariant(value));

        /// <summary>Closes the open record and returns it; false when none is open.</summary>
        public bool TryTake(out PendingRecord record)
        {
            if (!IsOpen) { record = default; return false; }
            string prefix = _k + ".f.";
            var fields = new Dictionary<string, string>();
            foreach (var kv in _s.Model.Values)
                if (kv.Key.StartsWith(prefix, StringComparison.Ordinal)) fields[kv.Key.Substring(prefix.Length)] = kv.Value;
            record = new PendingRecord(_s.GetLong(_k + ".ftu") == 1, fields);
            Discard();
            return true;
        }

        /// <summary>Closes the open record without resolving it.</summary>
        public void Discard()
        {
            _s.Remove(_k + ".open");
            _s.Remove(_k + ".ftu");
            _s.RemovePrefix(_k + ".f.");
        }
    }

    /// <summary>Allows the first occurrence of each id per scope. A new scope forgets the old ids.</summary>
    public readonly struct OncePer
    {
        readonly TrackingState _s;
        readonly string _k;
        /// <summary>Wraps <paramref name="key"/> in <paramref name="state"/>.</summary>
        public OncePer(TrackingState state, string key) { _s = state; _k = key; }

        /// <summary>True the first time <paramref name="id"/> is seen in <paramref name="scope"/>.</summary>
        public bool First(string scope, string id = "")
        {
            if (_s.GetString(_k + ".scope") != scope)
            {
                _s.SetString(_k + ".scope", scope);
                _s.Remove(_k + ".ids");
            }
            string ids = _s.GetString(_k + ".ids", "");
            string token = "|" + id + "|";
            if (ids.Contains(token)) return false;
            _s.SetString(_k + ".ids", ids + token);
            return true;
        }
    }
}

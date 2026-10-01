using System;
using System.Collections.Generic;
using System.Text;
using Hung.Base;
using UnityEngine;

namespace Hung.Analytics.Tracking
{
    /// <summary>
    /// Turns a logical event into its wire name and params: the ftu_ prefix, the A/B mode, the 40-char fallback and the B-name
    /// budget (spec section 6). Every event is sent as <see cref="AnalyticsCategory.Design"/>.
    /// </summary>
    public sealed class EventEmitter
    {
        /// <summary>Firebase's event-name length limit.</summary>
        public const int MaxNameLength = 40;

        sealed class Declaration
        {
            public string Template;
            public long MaxBNames;
        }

        readonly IAnalyticsService _sink;
        readonly TrackingSettings _settings;
        readonly Action<string> _warn;
        readonly Dictionary<string, Declaration> _declared = new Dictionary<string, Declaration>();
        readonly HashSet<string> _warned = new HashSet<string>();
        Dictionary<string, OutputMode> _resolved;
        ITrackingNamingProfile _profile;

        /// <summary>Creates an emitter sending through <paramref name="sink"/>.</summary>
        public EventEmitter(IAnalyticsService sink, TrackingSettings settings, Action<string> warn = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _warn = warn ?? Debug.LogWarning;
        }

        /// <summary>
        /// Declares a logical event. <paramref name="bTemplate"/> is its B-mode name with <c>{param}</c> holes, or null to always use A.
        /// <paramref name="maxBNames"/> is the worst-case count of distinct B names, before the ftu_ twin.
        /// </summary>
        public void Declare(string eventName, string bTemplate, long maxBNames)
        {
            _declared[eventName] = new Declaration { Template = bTemplate, MaxBNames = maxBNames };
            _resolved = null;
        }

        /// <summary>Sets the optional wire naming profile; null restores canonical output.</summary>
        public void SetNamingProfile(ITrackingNamingProfile profile)
        {
            _profile = profile;
            _resolved = null;
        }

        /// <summary>Sends with the ftu_ prefix when <paramref name="ftu"/> is true.</summary>
        public void Emit(string eventName, bool ftu, IDictionary<string, object> args) =>
            Send(eventName, ftu, ftu ? "ftu_" : "", Copy(args));

        /// <summary>Sends without a prefix and adds an <c>ftu</c> param of 1 or 0 (the tutorial style, T6).</summary>
        public void EmitTagged(string eventName, bool ftu, IDictionary<string, object> args)
        {
            var p = Copy(args);
            p["ftu"] = ftu ? 1 : 0;
            Send(eventName, ftu, "", p);
        }

        /// <summary>Sets a user property on every backend.</summary>
        public void SetUserProperty(string key, string value) => _sink.SetUserProperty(key, value);

        /// <summary>The effective mode of <paramref name="eventName"/> after the budget is applied. Undeclared events use A.</summary>
        public OutputMode ModeOf(string eventName)
        {
            Resolve();
            return _resolved.TryGetValue(eventName, out var m) ? m : OutputMode.A;
        }

        void Send(string eventName, bool ftu, string prefix,
            Dictionary<string, object> parameters)
        {
            var original = Copy(parameters);
            OutputMode configured = _settings.ModeFor(eventName);
            string fixedName = _profile == null ? eventName :
                _profile.FixedName(eventName, ftu, configured);
            string name = prefix + fixedName;
            if (ModeOf(eventName) == OutputMode.B && _declared.TryGetValue(eventName, out var d))
            {
                string template = _profile == null ? d.Template :
                    _profile.Template(eventName, ftu, d.Template);
                if (TryRender(template, parameters, out string rendered, out var rest))
                {
                    string candidate = prefix + rendered;
                    if (IsValidWireName(candidate))
                    {
                        name = candidate;
                        parameters = rest;
                    }
                    else WarnOnce(eventName + ".length",
                        "[Analytics] Invalid or oversize B name; using fixed name with all parameters.");
                }
                else WarnOnce(eventName + ".template",
                    "[Analytics] Missing or invalid B template value; using fixed name with all parameters.");
            }
            var translated = new Dictionary<string, object>();
            bool collision = false;
            foreach (var kv in parameters)
            {
                string key = _profile == null ? kv.Key :
                    _profile.ParameterKey(eventName, ftu, configured, kv.Key);
                if (string.IsNullOrEmpty(key) || translated.ContainsKey(key))
                { collision = true; break; }
                translated.Add(key, kv.Value);
            }
            if (collision || !IsValidWireName(name))
            {
                WarnOnce(eventName + ".profile", "[Analytics] Invalid naming profile; preserved canonical event and parameters.");
                name = prefix + eventName;
                translated = original;
            }
            _sink.LogEvent(name, AnalyticsCategory.Design,
                translated.Count == 0 ? null : translated);
        }

        static bool IsValidWireName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength ||
                !char.IsLetter(name[0]) || name[0] > 127) return false;
            foreach (char c in name)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') &&
                    !(c >= '0' && c <= '9') && c != '_') return false;
            return !name.StartsWith("firebase_", StringComparison.Ordinal) &&
                !name.StartsWith("google_", StringComparison.Ordinal) &&
                !name.StartsWith("ga_", StringComparison.Ordinal);
        }

        static bool TryRender(string template, Dictionary<string, object> args, out string rendered, out Dictionary<string, object> rest)
        {
            rest = new Dictionary<string, object>(args);
            rendered = null;
            if (string.IsNullOrEmpty(template)) return false;
            var sb = new StringBuilder(template.Length + 8);
            int i = 0;
            while (i < template.Length)
            {
                int open = template.IndexOf('{', i);
                if (open < 0) { sb.Append(template, i, template.Length - i); break; }
                int close = template.IndexOf('}', open);
                if (close < 0) return false;
                sb.Append(template, i, open - i);
                string key = template.Substring(open + 1, close - open - 1);
                if (!args.TryGetValue(key, out object value) || value == null) return false;
                sb.Append(AnalyticsText.ToInvariant(value).ToLowerInvariant());
                rest.Remove(key);
                i = close + 1;
            }
            rendered = sb.ToString();
            return true;
        }

        void Resolve()
        {
            if (_resolved != null) return;
            _resolved = new Dictionary<string, OutputMode>();
            var bEvents = new List<KeyValuePair<string, Declaration>>();
            long total = 0;
            foreach (var kv in _declared)
            {
                bool b = kv.Value.Template != null && _settings.ModeFor(kv.Key) == OutputMode.B;
                _resolved[kv.Key] = b ? OutputMode.B : OutputMode.A;
                if (!b) continue;
                bEvents.Add(kv);
                total += Names(kv.Value);
            }
            bEvents.Sort((x, y) => y.Value.MaxBNames.CompareTo(x.Value.MaxBNames));
            foreach (var kv in bEvents)
            {
                if (total <= _settings.bNameBudget) break;
                _resolved[kv.Key] = OutputMode.A;
                total -= Names(kv.Value);
                WarnOnce(kv.Key + ".budget", $"[Analytics] '{kv.Key}' forced to A mode: B names would exceed the budget of {_settings.bNameBudget}.");
            }
        }

        // Counts the ftu_ twin; capped so an unbounded declaration cannot overflow.
        static long Names(Declaration d) => Math.Min(d.MaxBNames, int.MaxValue) * 2;

        static Dictionary<string, object> Copy(IDictionary<string, object> args) =>
            args == null ? new Dictionary<string, object>() : new Dictionary<string, object>(args);

        void WarnOnce(string key, string message)
        {
            if (_warned.Add(key)) _warn(message);
        }
    }
}

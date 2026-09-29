using System;
using System.Collections.Generic;

namespace Hung.Analytics
{
    /// <summary>One event captured by <see cref="RecordingAnalyticsBackend"/>.</summary>
    public readonly struct RecordedEvent
    {
        /// <summary>Event name as the backend received it.</summary>
        public readonly string Name;
        /// <summary>Parameters as the backend received them; may be null.</summary>
        public readonly IReadOnlyDictionary<string, object> Parameters;
        /// <summary>Creates a record.</summary>
        public RecordedEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            Name = name;
            Parameters = parameters;
        }
    }

    /// <summary>One revenue call captured by <see cref="RecordingAnalyticsBackend"/>.</summary>
    public readonly struct RecordedRevenue
    {
        /// <summary>Ad network source.</summary>
        public readonly string Source;
        /// <summary>Revenue amount.</summary>
        public readonly double Value;
        /// <summary>ISO currency.</summary>
        public readonly string Currency;
        /// <summary>Neutral extra keys; may be null.</summary>
        public readonly IReadOnlyDictionary<string, string> Extra;
        /// <summary>Creates a record.</summary>
        public RecordedRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra)
        {
            Source = source;
            Value = value;
            Currency = currency;
            Extra = extra;
        }
    }

    /// <summary>
    /// Test double backend: records everything instead of calling an SDK, so tests exercise the real
    /// <c>AnalyticsService</c> routing. Can be told to throw to test failure isolation.
    /// </summary>
    public sealed class RecordingAnalyticsBackend : IAnalyticsBackend
    {
        /// <summary>Creates a recorder with the given backend id.</summary>
        public RecordingAnalyticsBackend(string id = "recording") { Id = id; }

        /// <inheritdoc/>
        public string Id { get; }
        /// <summary>Events received, in order.</summary>
        public List<RecordedEvent> Events { get; } = new List<RecordedEvent>();
        /// <summary>Revenue calls received, in order.</summary>
        public List<RecordedRevenue> Revenues { get; } = new List<RecordedRevenue>();
        /// <summary>Latest value per user property key.</summary>
        public Dictionary<string, string> UserProperties { get; } = new Dictionary<string, string>();
        /// <summary>Entry passed to <see cref="Initialize"/>; null until called.</summary>
        public AnalyticsBackendEntry InitializedWith { get; private set; }
        /// <summary>When set, <see cref="LogEvent"/> throws it.</summary>
        public Exception ThrowOnLog { get; set; }
        /// <summary>When set, <see cref="Initialize"/> throws it.</summary>
        public Exception ThrowOnInitialize { get; set; }

        /// <inheritdoc/>
        public void Initialize(AnalyticsBackendEntry entry, bool debugLog)
        {
            if (ThrowOnInitialize != null) throw ThrowOnInitialize;
            InitializedWith = entry;
        }

        /// <inheritdoc/>
        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            if (ThrowOnLog != null) throw ThrowOnLog;
            Events.Add(new RecordedEvent(name, parameters));
        }

        /// <inheritdoc/>
        public void SetUserProperty(string key, string value) => UserProperties[key] = value;

        /// <inheritdoc/>
        public void LogAdRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra)
            => Revenues.Add(new RecordedRevenue(source, value, currency, extra));
    }
}

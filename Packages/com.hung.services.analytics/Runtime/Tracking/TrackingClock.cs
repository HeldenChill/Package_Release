using System;

namespace Hung.Analytics.Tracking
{
    /// <summary>Wall clock used by tracking rules. Tests inject a fake.</summary>
    public interface ITrackingClock
    {
        /// <summary>Current UTC time.</summary>
        DateTime UtcNow { get; }
        /// <summary>Today's local calendar date (time part zero).</summary>
        DateTime LocalToday { get; }
    }

    /// <summary>The real device clock.</summary>
    public sealed class SystemTrackingClock : ITrackingClock
    {
        /// <inheritdoc/>
        public DateTime UtcNow => DateTime.UtcNow;
        /// <inheritdoc/>
        public DateTime LocalToday => DateTime.Now.Date;
    }
}

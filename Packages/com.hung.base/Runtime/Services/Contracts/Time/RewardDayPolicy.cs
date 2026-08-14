using System;

namespace Hung.Base
{
    /// <summary>
    /// Resolves reward days using a UTC reset offset.
    /// </summary>
    public readonly struct RewardDayPolicy
    {
        /// <summary>
        /// Creates a reward-day policy with reset offset in minutes after midnight UTC.
        /// </summary>
        public RewardDayPolicy(int resetOffsetMinutes)
        {
            if (resetOffsetMinutes < 0 || resetOffsetMinutes > 1439)
                throw new ArgumentOutOfRangeException(nameof(resetOffsetMinutes));

            ResetOffsetMinutes = resetOffsetMinutes;
        }

        /// <summary>
        /// Reset offset in minutes after midnight UTC.
        /// </summary>
        public int ResetOffsetMinutes { get; }

        /// <summary>
        /// Resolves the reward day for a UTC instant.
        /// </summary>
        public RewardDayKey Resolve(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Reward day resolution requires UTC.", nameof(utcNow));

            DateTime shifted = utcNow.AddMinutes(-ResetOffsetMinutes);
            return RewardDayKey.FromUtcDate(shifted.Date);
        }

        /// <summary>
        /// Returns the next UTC reset boundary after the supplied UTC instant.
        /// </summary>
        public DateTime GetNextBoundaryUtc(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Reward day boundary requires UTC.", nameof(utcNow));

            DateTime boundary = utcNow.Date.AddMinutes(ResetOffsetMinutes);
            if (utcNow >= boundary)
                boundary = boundary.AddDays(1);
            return DateTime.SpecifyKind(boundary, DateTimeKind.Utc);
        }
    }
}

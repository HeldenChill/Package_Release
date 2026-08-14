using System;

namespace Hung.Base
{
    /// <summary>
    /// Stable UTC reward date key encoded as yyyyMMdd.
    /// </summary>
    public readonly struct RewardDayKey : IEquatable<RewardDayKey>, IComparable<RewardDayKey>
    {
        /// <summary>
        /// Creates a reward day key from a yyyyMMdd integer.
        /// </summary>
        public RewardDayKey(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            Value = value;
        }

        /// <summary>
        /// Gets the yyyyMMdd value.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Creates a key from a UTC date or instant.
        /// </summary>
        public static RewardDayKey FromUtcDate(DateTime utcDate)
        {
            if (utcDate.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Reward day dates must be UTC.", nameof(utcDate));

            return new RewardDayKey(utcDate.Year * 10000 + utcDate.Month * 100 + utcDate.Day);
        }

        /// <summary>
        /// Converts this key to a midnight UTC date.
        /// </summary>
        public DateTime ToUtcDate()
        {
            int year = Value / 10000;
            int month = Value / 100 % 100;
            int day = Value % 100;
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <inheritdoc />
        public bool Equals(RewardDayKey other) => Value == other.Value;

        /// <inheritdoc />
        public int CompareTo(RewardDayKey other) => Value.CompareTo(other.Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is RewardDayKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value;

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D8");

        public static bool operator ==(RewardDayKey left, RewardDayKey right) => left.Equals(right);

        public static bool operator !=(RewardDayKey left, RewardDayKey right) => !left.Equals(right);
    }
}

using System;
using System.Globalization;
using System.Text;

namespace Hung.Base
{
    /// <summary>
    /// Deterministic opaque reward claim identity owned by the domain caller.
    /// </summary>
    public readonly struct RewardClaimId : IEquatable<RewardClaimId>
    {
        /// <summary>
        /// Creates a reward claim ID from a non-empty opaque value.
        /// </summary>
        public RewardClaimId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Reward claim ID cannot be empty.", nameof(value));

            Value = value;
        }

        /// <summary>
        /// Gets the opaque claim ID value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Creates a stable profile-scoped claim ID from canonical reward parts.
        /// </summary>
        public static RewardClaimId Create(string feature, string track, string cycle, string slot, string definition, string profile)
        {
            string canonical = string.Join("|",
                Normalize(feature, nameof(feature)),
                Normalize(track, nameof(track)),
                Normalize(cycle, nameof(cycle)),
                Normalize(slot, nameof(slot)),
                Normalize(definition, nameof(definition)),
                Normalize(profile, nameof(profile)));

            return new RewardClaimId(Fnv1A64(canonical));
        }

        /// <inheritdoc />
        public bool Equals(RewardClaimId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is RewardClaimId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        /// <inheritdoc />
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(RewardClaimId left, RewardClaimId right) => left.Equals(right);

        public static bool operator !=(RewardClaimId left, RewardClaimId right) => !left.Equals(right);

        private static string Normalize(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Reward claim ID parts cannot be empty.", name);
            return value.Trim().ToLowerInvariant();
        }

        private static string Fnv1A64(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }

            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}

using System;
using System.ComponentModel;
using Newtonsoft.Json;
using UnityEngine;

namespace Hung.Base
{
    /// <summary>
    /// Strongly-typed identifier for audio clips and cues.
    /// Strictly conforms to lowercase dot-separated grammar where package audio ids start with <c>base.</c>.
    /// </summary>
    [JsonConverter(typeof(AudioIdJsonConverter))]
    [TypeConverter(typeof(AudioIdTypeConverter))]
    [Serializable]
    public struct AudioId : IStableId, IEquatable<AudioId>, IComparable<AudioId>
    {
        [SerializeField] private string value;

        /// <summary>Initializes a new instance of the <see cref="AudioId"/> struct.</summary>
        /// <param name="value">The raw string identifier conforming to strict grammar.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> violates strict identifier grammar.</exception>
        public AudioId(string value)
        {
            if (!StableIdGrammar.IsValid(value))
                throw new ArgumentException($"Invalid audio id: '{value}'", nameof(value));

            this.value = value;
        }

        /// <summary>Gets the raw string value of this audio identifier.</summary>
        public string Value => value;

        /// <summary>Gets a value indicating whether this identifier has a valid value conforming to strict grammar.</summary>
        public bool IsValid => StableIdGrammar.IsValid(value);

        /// <summary>Parses a string into an <see cref="AudioId"/>.</summary>
        /// <param name="value">The raw string identifier.</param>
        /// <returns>The parsed identifier.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> violates strict grammar.</exception>
        public static AudioId Parse(string value)
        {
            return new AudioId(value);
        }

        /// <summary>Attempts to parse a string into an <see cref="AudioId"/>.</summary>
        /// <param name="value">The raw string identifier.</param>
        /// <param name="id">The parsed identifier, or default if invalid.</param>
        /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
        public static bool TryParse(string value, out AudioId id)
        {
            if (StableIdGrammar.IsValid(value))
            {
                id = new AudioId(value);
                return true;
            }

            id = default;
            return false;
        }

        /// <inheritdoc />
        public bool Equals(AudioId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is AudioId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
        }

        /// <inheritdoc />
        public int CompareTo(AudioId other)
        {
            return string.Compare(value, other.value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return value ?? string.Empty;
        }

        /// <summary>Equality operator using ordinal string comparison.</summary>
        public static bool operator ==(AudioId left, AudioId right)
        {
            return left.Equals(right);
        }

        /// <summary>Inequality operator using ordinal string comparison.</summary>
        public static bool operator !=(AudioId left, AudioId right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>Newtonsoft JSON converter for <see cref="AudioId"/>.</summary>
    public sealed class AudioIdJsonConverter : StableIdJsonConverter<AudioId>
    {
        /// <inheritdoc />
        protected override bool TryParse(string raw, out AudioId id) => AudioId.TryParse(raw, out id);
    }

    /// <summary>Type converter for <see cref="AudioId"/>.</summary>
    public sealed class AudioIdTypeConverter : StableIdTypeConverter<AudioId>
    {
        /// <inheritdoc />
        protected override AudioId Parse(string raw) => AudioId.Parse(raw);
    }
}

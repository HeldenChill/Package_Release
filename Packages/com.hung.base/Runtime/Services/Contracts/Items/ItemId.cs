using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Hung.Base
{
    [JsonConverter(typeof(ItemIdJsonConverter))]
    [TypeConverter(typeof(ItemIdTypeConverter))]
    [Serializable]
    public struct ItemId : IEquatable<ItemId>, IComparable<ItemId>
    {
        // Namespace segment ("base.gold") is optional and case is unrestricted, so catalog
        // ids authored straight from CSV display names ("Pump_Shotgun") parse as-is.
        // Equality below stays Ordinal: "Gold" and "gold" are DIFFERENT ids by design.
        private static readonly Regex Pattern = new(
            "^[a-zA-Z][a-zA-Z0-9_-]*(\\.[a-zA-Z][a-zA-Z0-9_-]*)?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        [SerializeField] private string value;

        public ItemId(string value)
        {
            if (!IsValidValue(value))
                throw new ArgumentException($"Invalid item id: '{value}'", nameof(value));

            this.value = value;
        }

        public string Value => value;

        public bool IsValid => IsValidValue(value);

        public static ItemId Parse(string value)
        {
            return new ItemId(value);
        }

        public static bool TryParse(string value, out ItemId id)
        {
            if (IsValidValue(value))
            {
                id = new ItemId(value);
                return true;
            }

            id = default;
            return false;
        }

        public bool Equals(ItemId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ItemId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
        }

        public int CompareTo(ItemId other)
        {
            return string.Compare(value, other.value, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return value ?? string.Empty;
        }

        public static bool operator ==(ItemId left, ItemId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemId left, ItemId right)
        {
            return !left.Equals(right);
        }

        private static bool IsValidValue(string value)
        {
            return !string.IsNullOrEmpty(value) && Pattern.IsMatch(value);
        }
    }
}

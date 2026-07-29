using System;

namespace Hung.Base
{
    public readonly struct PurchaseProductId : IEquatable<PurchaseProductId>
    {
        public const int MaxLength = 120;

        public PurchaseProductId(string value)
        {
            if (!IsCanonical(value))
                throw new ArgumentException("Purchase product id must be 1-120 chars using lowercase letters, digits, '.', '_' or '-'.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        public bool IsValid => IsCanonical(Value);

        public static PurchaseProductId Parse(string value) => new PurchaseProductId(value);

        public bool Equals(PurchaseProductId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PurchaseProductId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(PurchaseProductId left, PurchaseProductId right) => left.Equals(right);

        public static bool operator !=(PurchaseProductId left, PurchaseProductId right) => !left.Equals(right);

        private static bool IsCanonical(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool allowed =
                    c >= 'a' && c <= 'z' ||
                    c >= '0' && c <= '9' ||
                    c == '.' ||
                    c == '_' ||
                    c == '-';

                if (!allowed)
                    return false;
            }

            return true;
        }
    }
}

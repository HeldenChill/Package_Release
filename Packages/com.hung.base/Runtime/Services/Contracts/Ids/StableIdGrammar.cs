using System;

namespace Hung.Base
{
    /// <summary>Strict grammar validation and grouping utilities for typed stable identifiers.</summary>
    public static class StableIdGrammar
    {
        /// <summary>Maximum allowed character length for a strict stable identifier string.</summary>
        public const int MaxLength = 120;

        /// <summary>Validates that a string strictly conforms to lowercase dot-separated identifier grammar with at least two segments.</summary>
        /// <param name="value">The raw string to validate.</param>
        /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
                return false;

            int segmentCount = 0;
            int segmentLength = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '.')
                {
                    if (segmentLength == 0)
                        return false;

                    segmentCount++;
                    segmentLength = 0;
                    continue;
                }

                if (segmentLength == 0)
                {
                    if (c < 'a' || c > 'z')
                        return false;
                }
                else
                {
                    if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                        return false;
                }

                segmentLength++;
            }

            if (segmentLength == 0)
                return false;

            segmentCount++;
            return segmentCount >= 2;
        }

        /// <summary>Extracts the root group segment before the first period separator.</summary>
        /// <param name="value">The identifier string.</param>
        /// <returns>The group string, or empty string if no separator exists.</returns>
        public static string GetGroup(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            int index = value.IndexOf('.', StringComparison.Ordinal);
            return index > 0 ? value.Substring(0, index) : string.Empty;
        }
    }
}

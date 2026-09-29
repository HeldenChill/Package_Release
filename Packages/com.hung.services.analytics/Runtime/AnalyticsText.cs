using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hung.Analytics
{
    /// <summary>SDK-free string helpers shared by backends. Always culture-invariant.</summary>
    public static class AnalyticsText
    {
        /// <summary>
        /// Firebase-safe name: chars outside [A-Za-z0-9_] become '_', a non-letter start gets an "e_" prefix,
        /// then truncated to <paramref name="maxLength"/>. Null/empty becomes "unnamed".
        /// </summary>
        public static string Sanitize(string name, int maxLength)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed";
            var sb = new StringBuilder(name.Length + 2);
            if (!IsAsciiLetter(name[0])) sb.Append("e_");
            foreach (char c in name)
                sb.Append(IsAsciiLetter(c) || (c >= '0' && c <= '9') || c == '_' ? c : '_');
            return sb.Length > maxLength ? sb.ToString(0, maxLength) : sb.ToString();
        }

        /// <summary>Invariant-culture text for a parameter value; bools are "true"/"false", null is "".</summary>
        public static string ToInvariant(object value) => value switch
        {
            null => "",
            string s => s,
            bool b => b ? "true" : "false",
            System.IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };

        /// <summary>Copies parameters into a string dictionary (AppsFlyer shape). Null in, null out.</summary>
        public static Dictionary<string, string> ToStringDictionary(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null) return null;
            var result = new Dictionary<string, string>(parameters.Count);
            foreach (var kv in parameters) result[kv.Key] = ToInvariant(kv.Value);
            return result;
        }

        /// <summary>
        /// Flat JSON object (AppMetrica shape). Finite numbers and bools are written raw, everything else
        /// as an escaped string. Null or empty in, null out.
        /// </summary>
        public static string ToJson(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return null;
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in parameters)
            {
                if (!first) sb.Append(',');
                first = false;
                AppendString(sb, kv.Key);
                sb.Append(':');
                if (kv.Value == null) sb.Append("null");
                else if (kv.Value is bool b) sb.Append(b ? "true" : "false");
                else if (IsFiniteNumber(kv.Value)) sb.Append(ToInvariant(kv.Value));
                else AppendString(sb, ToInvariant(kv.Value));
            }
            return sb.Append('}').ToString();
        }

        static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        static bool IsFiniteNumber(object value) => value switch
        {
            byte or sbyte or short or ushort or int or uint or long or ulong or decimal => true,
            float f => !float.IsNaN(f) && !float.IsInfinity(f),
            double d => !double.IsNaN(d) && !double.IsInfinity(d),
            _ => false,
        };

        static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}

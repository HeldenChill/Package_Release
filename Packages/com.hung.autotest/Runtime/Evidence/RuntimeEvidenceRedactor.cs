using System.Text.RegularExpressions;

namespace Hung.AutoTest
{
    public static class RuntimeEvidenceRedactor
    {
        private static readonly Regex SensitivePairs = new Regex(
            @"(?i)\b(receipt|token|password|secret|keyalias|keystore)\b(\s*[:=]\s*)(""?)[^,\s""}]+(""?){0,1}",
            RegexOptions.Compiled);

        public static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return SensitivePairs.Replace(value, match =>
            {
                string quote = match.Groups[3].Value;
                return match.Groups[1].Value + match.Groups[2].Value + quote + "[REDACTED]" + quote;
            });
        }
    }
}

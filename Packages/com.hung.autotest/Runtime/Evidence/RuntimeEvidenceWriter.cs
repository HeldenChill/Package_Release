using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Hung.AutoTest
{
    public static class RuntimeEvidenceWriter
    {
        public static string WriteJson(RuntimeEvidenceRecord record, string root)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            Directory.CreateDirectory(root);
            string scenario = Sanitize(record.scenarioId);
            string run = Sanitize(record.runId);

            for (int attempt = 0; attempt < 32; attempt++)
            {
                string utc = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
                string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);
                string path = Path.Combine(root, $"{scenario}-{run}-{utc}-{nonce}.json");
                try
                {
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.Write(RuntimeEvidenceRedactor.Redact(JsonUtility.ToJson(record, true)));
                    }

                    return path;
                }
                catch (IOException) when (attempt < 31)
                {
                }
            }

            throw new IOException("Failed to create append-only runtime evidence file.");
        }

        public static string ComputeSha256(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "unknown";

            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return builder.ToString();
        }
    }
}

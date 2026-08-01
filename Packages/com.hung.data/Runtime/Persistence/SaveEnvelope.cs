using System;
using System.Globalization;
using System.Text;
using Hung.Base.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hung.Data.Persistence
{
    public sealed class SaveEnvelope
    {
        public const int CurrentFormatVersion = 1;

        [JsonProperty("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("payloadSchemaVersion")]
        public int PayloadSchemaVersion { get; set; }

        [JsonProperty("writtenAtUtc")]
        public string WrittenAtUtc { get; set; }

        [JsonProperty("payloadEncoding")]
        public string PayloadEncoding { get; set; }

        [JsonProperty("payloadProtection")]
        public string PayloadProtection { get; set; }

        [JsonProperty("payload")]
        public JToken Payload { get; set; }

        [JsonProperty("integrity")]
        public string Integrity { get; set; }

        public static SaveEnvelope Create(string saveKey, int payloadSchemaVersion, DateTime writtenAtUtc, SaveEncodedPayload payload, string protectionId)
        {
            SaveDefinition.ValidateKey(saveKey);
            if (payloadSchemaVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(payloadSchemaVersion));

            JToken token = payload.EncodingId == "json"
                ? JToken.Parse(Encoding.UTF8.GetString(payload.Bytes))
                : Convert.ToBase64String(payload.Bytes);

            return new SaveEnvelope
            {
                FormatVersion = CurrentFormatVersion,
                SaveKey = saveKey,
                PayloadSchemaVersion = payloadSchemaVersion,
                WrittenAtUtc = writtenAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                PayloadEncoding = payload.EncodingId,
                PayloadProtection = protectionId,
                Payload = token
            };
        }

        public static SaveEnvelope FromJson(string json)
        {
            var envelope = JsonConvert.DeserializeObject<SaveEnvelope>(json);
            if (envelope == null)
                throw new JsonSerializationException("Save envelope is null.");
            return envelope;
        }

        public string ToJson()
        {
            var root = new JObject
            {
                ["formatVersion"] = FormatVersion,
                ["saveKey"] = SaveKey,
                ["payloadSchemaVersion"] = PayloadSchemaVersion,
                ["writtenAtUtc"] = WrittenAtUtc,
                ["payloadEncoding"] = PayloadEncoding,
                ["payloadProtection"] = PayloadProtection,
                ["payload"] = Payload,
                ["integrity"] = Integrity
            };
            return root.ToString(Formatting.None);
        }

        public byte[] GetPayloadBytes()
        {
            if (PayloadEncoding == "json")
                return Encoding.UTF8.GetBytes(Payload.ToString(Formatting.None));

            if (Payload.Type != JTokenType.String)
                throw new JsonSerializationException("Compressed payload must be a Base64 string.");

            return Convert.FromBase64String(Payload.Value<string>());
        }

        public byte[] GetAuthenticatedBytes()
        {
            var builder = new StringBuilder();
            builder.Append(FormatVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            builder.Append(SaveKey).Append('\n');
            builder.Append(PayloadSchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            builder.Append(PayloadEncoding).Append('\n');
            builder.Append(PayloadProtection).Append('\n');
            return Combine(Encoding.UTF8.GetBytes(builder.ToString()), GetPayloadBytes());
        }

        public SaveValidationResult ValidateFor(string expectedSaveKey)
        {
            if (FormatVersion > CurrentFormatVersion)
                return SaveValidationResult.Invalid("SAVE_ENVELOPE_NEWER_THAN_CLIENT");
            if (!string.Equals(SaveKey, expectedSaveKey, StringComparison.Ordinal))
                return SaveValidationResult.Invalid("SAVE_KEY_MISMATCH");
            if (PayloadSchemaVersion < 0 || Payload == null || string.IsNullOrEmpty(PayloadEncoding) || string.IsNullOrEmpty(PayloadProtection))
                return SaveValidationResult.Invalid("SAVE_ENVELOPE_INVALID");
            return SaveValidationResult.Valid();
        }

        private static byte[] Combine(byte[] prefix, byte[] payload)
        {
            var result = new byte[prefix.Length + payload.Length];
            Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
            Buffer.BlockCopy(payload, 0, result, prefix.Length, payload.Length);
            return result;
        }
    }
}

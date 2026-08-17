using System;
using Newtonsoft.Json.Linq;

namespace Hung.Base.Persistence
{
    public readonly struct SaveValidationResult
    {
        public SaveValidationResult(bool success, string diagnosticCode = null, string message = null)
        {
            Success = success;
            DiagnosticCode = diagnosticCode;
            Message = message;
        }

        public bool Success { get; }
        public string DiagnosticCode { get; }
        public string Message { get; }

        public static SaveValidationResult Valid() => new SaveValidationResult(true);
        public static SaveValidationResult Invalid(string diagnosticCode, string message = null) => new SaveValidationResult(false, diagnosticCode, message);
    }

    public readonly struct SaveEncodedPayload
    {
        public SaveEncodedPayload(string encodingId, byte[] bytes)
        {
            EncodingId = encodingId;
            Bytes = bytes ?? Array.Empty<byte>();
        }

        public string EncodingId { get; }
        public byte[] Bytes { get; }
    }

    public readonly struct SaveSecretKeyResult
    {
        public SaveSecretKeyResult(bool success, byte[] key, string errorCode = null)
        {
            Success = success;
            Key = key;
            ErrorCode = errorCode;
        }

        public bool Success { get; }
        public byte[] Key { get; }
        public string ErrorCode { get; }
    }

    public interface ISaveCodec
    {
        string EncodingId { get; }
        SaveEncodedPayload Encode(byte[] jsonBytes);
        byte[] Decode(SaveEncodedPayload payload);
    }

    public interface ISaveProtector
    {
        string ProtectionId { get; }
        string Protect(byte[] authenticatedBytes);
        bool Verify(byte[] authenticatedBytes, string tag);
    }

    public interface ISecretKeyProvider
    {
        SaveSecretKeyResult GetOrCreateKey(string purpose);
    }

    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        JObject Migrate(JObject source);
    }
}

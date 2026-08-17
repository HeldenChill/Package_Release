using System;
using System.Security.Cryptography;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    public sealed class Sha256SaveProtector : ISaveProtector
    {
        public string ProtectionId => "sha256";

        public string Protect(byte[] authenticatedBytes) => Convert.ToBase64String(Hash(authenticatedBytes));

        public bool Verify(byte[] authenticatedBytes, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;
            return FixedEquals(Hash(authenticatedBytes), Convert.FromBase64String(tag));
        }

        private static byte[] Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(bytes);
        }

        internal static bool FixedEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
#if NETSTANDARD2_1_OR_GREATER
            return CryptographicOperations.FixedTimeEquals(left, right);
#else
            int diff = 0;
            for (int i = 0; i < left.Length; i++)
                diff |= left[i] ^ right[i];
            return diff == 0;
#endif
        }
    }

    public sealed class HmacSha256SaveProtector : ISaveProtector
    {
        private readonly byte[] key;

        public HmacSha256SaveProtector(byte[] key)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("HMAC key must be 32 bytes.", nameof(key));
            this.key = (byte[])key.Clone();
        }

        public string ProtectionId => "hmac-sha256";

        public string Protect(byte[] authenticatedBytes)
        {
            using var hmac = new HMACSHA256(key);
            return Convert.ToBase64String(hmac.ComputeHash(authenticatedBytes));
        }

        public bool Verify(byte[] authenticatedBytes, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;
            using var hmac = new HMACSHA256(key);
            return Sha256SaveProtector.FixedEquals(hmac.ComputeHash(authenticatedBytes), Convert.FromBase64String(tag));
        }
    }
}

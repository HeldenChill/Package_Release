using System;
using System.IO;
using System.Security.Cryptography;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    public sealed class LocalSecretKeyProvider : ISecretKeyProvider
    {
        private readonly string keyDirectory;

        public LocalSecretKeyProvider(string keyDirectory)
        {
            this.keyDirectory = keyDirectory ?? throw new ArgumentNullException(nameof(keyDirectory));
        }

        public SaveSecretKeyResult GetOrCreateKey(string purpose)
        {
            try
            {
                SaveDefinition.ValidateKey(purpose);
                Directory.CreateDirectory(keyDirectory);
                string path = Path.Combine(keyDirectory, purpose + ".key");
                if (File.Exists(path))
                {
                    byte[] existing = File.ReadAllBytes(path);
                    return existing.Length == 32
                        ? new SaveSecretKeyResult(true, existing)
                        : new SaveSecretKeyResult(false, null, "SAVE_KEY_UNAVAILABLE");
                }

                byte[] key = new byte[32];
                RandomNumberGenerator.Fill(key);
                string tempPath = Path.Combine(keyDirectory, purpose + "." + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllBytes(tempPath, key);
                File.Move(tempPath, path);
                return new SaveSecretKeyResult(true, key);
            }
            catch
            {
                return new SaveSecretKeyResult(false, null, "SAVE_KEY_UNAVAILABLE");
            }
        }
    }
}

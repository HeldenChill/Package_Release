using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hung.Base.Persistence
{
    public abstract class SaveDefinition
    {
        protected SaveDefinition(
            Type modelType,
            string key,
            int currentSchemaVersion,
            IEnumerable<ISaveMigration> migrations,
            IEnumerable<string> legacyPlayerPrefsKeys,
            ISaveCodec codec,
            ISaveProtector protector,
            SaveFailurePolicy failurePolicy)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
            Key = ValidateKey(key);
            if (currentSchemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(currentSchemaVersion));

            CurrentSchemaVersion = currentSchemaVersion;
            Migrations = (migrations ?? Array.Empty<ISaveMigration>()).ToArray();
            LegacyPlayerPrefsKeys = ValidateLegacyKeys(legacyPlayerPrefsKeys).ToArray();
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Protector = protector ?? throw new ArgumentNullException(nameof(protector));
            FailurePolicy = failurePolicy;
        }

        public Type ModelType { get; }
        public string Key { get; }
        public int CurrentSchemaVersion { get; }
        public IReadOnlyList<ISaveMigration> Migrations { get; }
        public IReadOnlyList<string> LegacyPlayerPrefsKeys { get; }
        public ISaveCodec Codec { get; }
        public ISaveProtector Protector { get; }
        public SaveFailurePolicy FailurePolicy { get; }

        public static string ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Save key cannot be empty.", nameof(key));
            if (key == "." || key == ".." || key.Length > 120 || Path.IsPathRooted(key) || key.Contains("/") || key.Contains("\\"))
                throw new ArgumentException($"Invalid save key '{key}'.", nameof(key));
            if (key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"Invalid save key '{key}'.", nameof(key));
            return key;
        }

        private static IEnumerable<string> ValidateLegacyKeys(IEnumerable<string> keys)
        {
            if (keys == null)
                yield break;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in keys)
            {
                string valid = ValidateKey(key);
                if (!seen.Add(valid))
                    throw new ArgumentException($"Duplicate legacy key '{valid}'.", nameof(keys));
                yield return valid;
            }
        }
    }

    public sealed class SaveDefinition<T> : SaveDefinition where T : new()
    {
        public SaveDefinition(
            string key,
            int currentSchemaVersion,
            Func<T> createDefault,
            Func<T, SaveValidationResult> validate,
            IEnumerable<ISaveMigration> migrations,
            IEnumerable<string> legacyPlayerPrefsKeys,
            ISaveCodec codec,
            ISaveProtector protector,
            SaveFailurePolicy failurePolicy)
            : base(typeof(T), key, currentSchemaVersion, migrations, legacyPlayerPrefsKeys, codec, protector, failurePolicy)
        {
            CreateDefault = createDefault ?? throw new ArgumentNullException(nameof(createDefault));
            Validate = validate ?? throw new ArgumentNullException(nameof(validate));
        }

        public Func<T> CreateDefault { get; }
        public Func<T, SaveValidationResult> Validate { get; }
    }
}

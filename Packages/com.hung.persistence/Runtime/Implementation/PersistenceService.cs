using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hung.Base.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hung.Data.Persistence
{
    public sealed class PersistenceService : IPersistenceService, ICanonicalExistenceQuery
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Culture = System.Globalization.CultureInfo.InvariantCulture
        };

        private readonly ISaveStore store;
        private readonly ILegacySaveSource legacy;
        private readonly ISaveDiagnostics diagnostics;
        private readonly ICanonicalEvidenceStore evidence;
        private readonly Dictionary<Type, SaveDefinition> definitionsByType = new Dictionary<Type, SaveDefinition>();

        public PersistenceService(ISaveStore store, ILegacySaveSource legacy = null, ISaveDiagnostics diagnostics = null, ICanonicalEvidenceStore evidence = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.legacy = legacy;
            this.diagnostics = diagnostics ?? NullSaveDiagnostics.Instance;
            this.evidence = evidence;
        }

        public bool CanonicalExistenceProven(string key) => evidence != null && evidence.HasReceipt(key);

        public void Register<T>(SaveDefinition<T> definition) where T : new()
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            definitionsByType[typeof(T)] = definition;
        }

        public bool TryGetDefinition<T>(string requestedKey, out SaveDefinition<T> definition) where T : new()
        {
            if (definitionsByType.TryGetValue(typeof(T), out SaveDefinition value))
            {
                var typed = (SaveDefinition<T>)value;
                if (string.IsNullOrEmpty(requestedKey) || typed.Key == requestedKey || typed.LegacyPlayerPrefsKeys.Contains(requestedKey))
                {
                    definition = typed;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public SaveResult Save<T>(SaveDefinition<T> definition, T value) where T : new()
        {
            try
            {
                SaveValidationResult validation = definition.Validate(value);
                if (!validation.Success)
                    return new SaveResult(false, validation.DiagnosticCode);

                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, Formatting.None, JsonSettings));
                SaveEncodedPayload payload = definition.Codec.Encode(jsonBytes);
                SaveEnvelope envelope = SaveEnvelope.Create(definition.Key, definition.CurrentSchemaVersion, DateTime.UtcNow, payload, definition.Protector.ProtectionId);
                envelope.Integrity = definition.Protector.Protect(envelope.GetAuthenticatedBytes());
                byte[] bytes = Encoding.UTF8.GetBytes(envelope.ToJson());
                SaveStoreWriteResult write = store.Write(definition.Key, bytes);
                if (write.Success)
                {
                    // Commit the receipt AFTER a successful canonical write, never before.
                    // A crash between write and receipt leaves primary present, so the legacy
                    // gate is already closed by primary.Exists; the receipt lands on the next
                    // successful save. The inverse order would, on a crash, permanently block
                    // legacy import for a user whose canonical write never landed -
                    // a worse, unrecoverable failure.
                    evidence?.CommitReceipt(definition.Key, definition.CurrentSchemaVersion, DateTime.UtcNow);
                }
                return new SaveResult(write.Success, write.Success ? null : write.ErrorCode, write.Path);
            }
            catch
            {
                return new SaveResult(false, "SAVE_WRITE_FAILED_PREVIOUS_RETAINED");
            }
        }

        public LoadResult<T> Load<T>(SaveDefinition<T> definition) where T : new()
        {
            SaveStoreReadResult primary = store.ReadPrimary(definition.Key);
            if (primary.Success && primary.Exists)
            {
                LoadResult<T> result = TryLoadBytes(definition, primary.Content.ToArray(), SaveDataSource.Primary);
                if (result.Success || result.Recovery == SaveRecoveryState.UnsupportedNewerVersion)
                    return result;

                store.QuarantinePrimary(definition.Key, primary.Content.ToArray(), result.DiagnosticCode ?? "SAVE_PRIMARY_CORRUPT");
            }

            SaveStoreReadResult backup = store.ReadBackup(definition.Key);
            if (backup.Success && backup.Exists)
            {
                LoadResult<T> result = TryLoadBytes(definition, backup.Content.ToArray(), SaveDataSource.Backup);
                if (result.Success)
                {
                    store.RestoreBackup(definition.Key);
                    return new LoadResult<T>(true, result.Value, "SAVE_BACKUP_RESTORED", SaveDataSource.Backup, SaveRecoveryState.BackupRestored);
                }

                if (result.Recovery == SaveRecoveryState.UnsupportedNewerVersion)
                    return result;
                store.QuarantineBackup(definition.Key, backup.Content.ToArray(), result.DiagnosticCode ?? "SAVE_PRIMARY_CORRUPT");
            }

            if ((!primary.Exists && !backup.Exists && !CanonicalExistenceProven(definition.Key)) && TryLoadLegacy(definition, out LoadResult<T> legacyResult))
                return legacyResult;

            if (definition.FailurePolicy == SaveFailurePolicy.FailClosed && (primary.Exists || backup.Exists))
                return new LoadResult<T>(false, default, "SAVE_PRIMARY_CORRUPT", SaveDataSource.None, SaveRecoveryState.Unrecoverable);

            T defaultValue = definition.CreateDefault();
            SaveResult save = Save(definition, defaultValue);
            return new LoadResult<T>(save.Success, defaultValue, save.Success ? "SAVE_DEFAULT_CREATED" : save.DiagnosticCode, SaveDataSource.Default, save.Success ? SaveRecoveryState.DefaultCreated : SaveRecoveryState.Unrecoverable);
        }

        private bool TryLoadLegacy<T>(SaveDefinition<T> definition, out LoadResult<T> result) where T : new()
        {
            if (legacy != null)
            {
                foreach (string key in definition.LegacyPlayerPrefsKeys)
                {
                    if (!legacy.TryRead(key, out string json))
                        continue;
                    JObject raw = JObject.Parse(json);
                    var registry = new SaveMigrationRegistry(definition.Migrations);
                    SaveMigrationResult migrated = registry.Migrate(raw, 0, definition.CurrentSchemaVersion);
                    if (!migrated.Success)
                    {
                        result = new LoadResult<T>(false, default, migrated.ErrorCode, SaveDataSource.LegacyPlayerPrefs, SaveRecoveryState.Unrecoverable);
                        return true;
                    }

                    T value = migrated.Value.ToObject<T>(JsonSerializer.Create(JsonSettings));
                    SaveValidationResult validation = definition.Validate(value);
                    if (!validation.Success)
                    {
                        result = new LoadResult<T>(false, default, validation.DiagnosticCode, SaveDataSource.LegacyPlayerPrefs, SaveRecoveryState.Unrecoverable);
                        return true;
                    }

                    Save(value is null ? definition.CreateDefault() : value, definition);
                    result = new LoadResult<T>(true, value, "SAVE_LEGACY_IMPORTED", SaveDataSource.LegacyPlayerPrefs, SaveRecoveryState.Migrated);
                    return true;
                }
            }

            result = default;
            return false;
        }

        private LoadResult<T> TryLoadBytes<T>(SaveDefinition<T> definition, byte[] bytes, SaveDataSource source) where T : new()
        {
            try
            {
                string json = Encoding.UTF8.GetString(bytes);
                SaveEnvelope envelope = SaveEnvelope.FromJson(json);
                SaveValidationResult envelopeValidation = envelope.ValidateFor(definition.Key);
                if (!envelopeValidation.Success)
                {
                    SaveRecoveryState recovery = envelopeValidation.DiagnosticCode == "SAVE_ENVELOPE_NEWER_THAN_CLIENT"
                        ? SaveRecoveryState.UnsupportedNewerVersion
                        : SaveRecoveryState.PrimaryQuarantined;
                    return new LoadResult<T>(false, default, envelopeValidation.DiagnosticCode, source, recovery);
                }

                if (envelope.PayloadSchemaVersion > definition.CurrentSchemaVersion)
                    return new LoadResult<T>(false, default, "SAVE_SCHEMA_NEWER_THAN_CLIENT", source, SaveRecoveryState.UnsupportedNewerVersion);
                if (!definition.Protector.Verify(envelope.GetAuthenticatedBytes(), envelope.Integrity))
                    return new LoadResult<T>(false, default, envelope.PayloadProtection == "hmac-sha256" ? "SAVE_HMAC_MISMATCH" : "SAVE_CHECKSUM_MISMATCH", source, SaveRecoveryState.PrimaryQuarantined);

                byte[] payloadBytes = definition.Codec.Decode(new SaveEncodedPayload(envelope.PayloadEncoding, envelope.GetPayloadBytes()));
                JObject payload = JObject.Parse(Encoding.UTF8.GetString(payloadBytes));
                var registry = new SaveMigrationRegistry(definition.Migrations);
                SaveMigrationResult migrated = registry.Migrate(payload, envelope.PayloadSchemaVersion, definition.CurrentSchemaVersion);
                if (!migrated.Success)
                    return new LoadResult<T>(false, default, migrated.ErrorCode, source, SaveRecoveryState.PrimaryQuarantined);

                T value = migrated.Value.ToObject<T>(JsonSerializer.Create(JsonSettings));
                SaveValidationResult validation = definition.Validate(value);
                if (!validation.Success)
                    return new LoadResult<T>(false, default, validation.DiagnosticCode, source, SaveRecoveryState.PrimaryQuarantined);

                if (envelope.PayloadSchemaVersion != definition.CurrentSchemaVersion)
                    Save(definition, value);
                return new LoadResult<T>(true, value, null, source, SaveRecoveryState.None);
            }
            catch
            {
                return new LoadResult<T>(false, default, "SAVE_PRIMARY_CORRUPT", source, SaveRecoveryState.PrimaryQuarantined);
            }
        }

        private SaveResult Save<T>(T value, SaveDefinition<T> definition) where T : new() => Save(definition, value);
    }
}

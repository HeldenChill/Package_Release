using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base.Persistence;
using Newtonsoft.Json.Linq;

namespace Hung.Data.Persistence
{
    public readonly struct SaveMigrationResult
    {
        public SaveMigrationResult(bool success, JObject value, string errorCode = null)
        {
            Success = success;
            Value = value;
            ErrorCode = errorCode;
        }

        public bool Success { get; }
        public JObject Value { get; }
        public string ErrorCode { get; }
    }

    public sealed class SaveMigrationRegistry
    {
        private readonly Dictionary<int, ISaveMigration> migrations;

        public SaveMigrationRegistry(IEnumerable<ISaveMigration> migrations)
        {
            this.migrations = new Dictionary<int, ISaveMigration>();
            foreach (ISaveMigration migration in migrations ?? Enumerable.Empty<ISaveMigration>())
            {
                if (migration.ToVersion != migration.FromVersion + 1)
                    throw new ArgumentException("Save migrations must advance exactly one schema version.");
                if (this.migrations.ContainsKey(migration.FromVersion))
                    throw new ArgumentException($"Duplicate migration from schema {migration.FromVersion}.");
                this.migrations.Add(migration.FromVersion, migration);
            }
        }

        public SaveMigrationResult Migrate(JObject source, int loadedSchemaVersion, int currentSchemaVersion)
        {
            if (loadedSchemaVersion > currentSchemaVersion)
                return new SaveMigrationResult(false, source, "SAVE_SCHEMA_NEWER_THAN_CLIENT");
            if (loadedSchemaVersion == currentSchemaVersion)
                return new SaveMigrationResult(true, (JObject)source.DeepClone());

            JObject current = (JObject)source.DeepClone();
            for (int version = loadedSchemaVersion; version < currentSchemaVersion; version++)
            {
                if (!migrations.TryGetValue(version, out ISaveMigration migration))
                    return new SaveMigrationResult(false, source, "SAVE_MIGRATION_CHAIN_MISSING");
                try
                {
                    current = migration.Migrate((JObject)current.DeepClone());
                }
                catch
                {
                    return new SaveMigrationResult(false, source, "SAVE_MIGRATION_FAILED");
                }
            }

            return new SaveMigrationResult(true, current);
        }
    }

    public sealed class LegacyRawJsonToSchemaOneMigration : ISaveMigration
    {
        public int FromVersion => 0;
        public int ToVersion => 1;

        public JObject Migrate(JObject source)
        {
            return (JObject)source.DeepClone();
        }
    }
}

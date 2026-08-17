using System;
using System.Collections.Generic;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Hung.Persistence.Tests
{
    [TestFixture]
    public class LegacyImportMechanismTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "HungPersistenceLegacyTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        [Test]
        public void Load_LegacyFilePresent_ImportsIntoPrimaryAndLeavesLegacyFileUntouched()
        {
            byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes("{\"Count\":7}");
            string legacyPath = Path.Combine(tempRoot, "sample-slot");
            File.WriteAllBytes(legacyPath, originalBytes);

            var legacySource = new FileLegacySaveSource(tempRoot);
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store, legacySource);
            SaveDefinition<SampleState> definition = Definition("sample-slot");

            LoadResult<SampleState> result = service.Load(definition);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(SaveDataSource.LegacyPlayerPrefs, result.Source);
            Assert.AreEqual(SaveRecoveryState.Migrated, result.Recovery);
            Assert.AreEqual(7, result.Value.Count);
            Assert.IsTrue(store.Primary.ContainsKey("sample-slot-canonical"));
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(legacyPath));
        }

        [Test]
        public void Load_LegacyFileAbsent_FallsThroughToDefault()
        {
            var legacySource = new FileLegacySaveSource(tempRoot);
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store, legacySource);
            SaveDefinition<SampleState> definition = Definition("missing-slot");

            LoadResult<SampleState> result = service.Load(definition);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(SaveDataSource.Default, result.Source);
            Assert.AreEqual(SaveRecoveryState.DefaultCreated, result.Recovery);
        }

        [Test]
        public void Load_LegacyFileMalformed_ImportFailsAndSourceFileUntouched()
        {
            const string malformed = "not json at all {{{";
            string legacyPath = Path.Combine(tempRoot, "bad-slot");
            File.WriteAllText(legacyPath, malformed);

            var legacySource = new FileLegacySaveSource(tempRoot);
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store, legacySource);
            SaveDefinition<SampleState> definition = Definition("bad-slot");

            Assert.That(() => service.Load(definition), Throws.Exception);
            Assert.AreEqual(malformed, File.ReadAllText(legacyPath));
            Assert.IsFalse(store.Primary.ContainsKey("bad-slot"));
        }

        private static SaveDefinition<SampleState> Definition(string legacyKey) => new SaveDefinition<SampleState>(
            key: "sample-slot-canonical",
            currentSchemaVersion: 1,
            createDefault: () => new SampleState(),
            validate: _ => SaveValidationResult.Valid(),
            migrations: new ISaveMigration[] { new IdentityMigration() },
            legacyPlayerPrefsKeys: new[] { legacyKey },
            codec: new PlainJsonSaveCodec(),
            protector: new Sha256SaveProtector(),
            failurePolicy: SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);

        private sealed class SampleState
        {
            public int Count { get; set; }
        }

        private sealed class IdentityMigration : ISaveMigration
        {
            public int FromVersion => 0;
            public int ToVersion => 1;
            public JObject Migrate(JObject source) => source;
        }

        private sealed class InMemorySaveStore : ISaveStore
        {
            public readonly Dictionary<string, byte[]> Primary = new Dictionary<string, byte[]>();
            public readonly Dictionary<string, byte[]> Backup = new Dictionary<string, byte[]>();

            public SaveStoreReadResult ReadPrimary(string key) => Primary.TryGetValue(key, out byte[] bytes) ? SaveStoreReadResult.Found(bytes) : SaveStoreReadResult.Missing();
            public SaveStoreReadResult ReadBackup(string key) => Backup.TryGetValue(key, out byte[] bytes) ? SaveStoreReadResult.Found(bytes) : SaveStoreReadResult.Missing();

            public SaveStoreWriteResult Write(string key, byte[] content)
            {
                if (Primary.TryGetValue(key, out byte[] existing))
                    Backup[key] = existing;
                Primary[key] = content;
                return SaveStoreWriteResult.Succeeded(key);
            }

            public SaveStoreWriteResult RestoreBackup(string key)
            {
                if (!Backup.TryGetValue(key, out byte[] bytes))
                    return SaveStoreWriteResult.Failed("SAVE_BACKUP_MISSING");
                Primary[key] = bytes;
                return SaveStoreWriteResult.Succeeded(key);
            }

            public SaveStoreWriteResult QuarantinePrimary(string key, byte[] content, string reason)
            {
                Primary.Remove(key);
                return SaveStoreWriteResult.Succeeded(reason);
            }

            public SaveStoreWriteResult QuarantineBackup(string key, byte[] content, string reason)
            {
                Backup.Remove(key);
                return SaveStoreWriteResult.Succeeded(reason);
            }
        }
    }
}

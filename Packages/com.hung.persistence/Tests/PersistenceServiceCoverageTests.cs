using System;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Hung.Persistence.Tests
{
    /// <summary>
    /// Product-free replacement coverage (plan §9.1 "coverage consequence"): validation
    /// rejection and end-to-end migration chain continuity through PersistenceService.
    /// </summary>
    [TestFixture]
    public class PersistenceServiceCoverageTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "HungPersistenceCoverageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        [Test]
        public void Save_ValidatorRejects_SaveFailsAndPrimaryUnchanged()
        {
            var store = new FileSaveStore(tempRoot, new SystemFileSaveOperations());
            var service = new PersistenceService(store, null);
            var definition = new SaveDefinition<SampleState>(
                key: "validation-reject-slot",
                currentSchemaVersion: 1,
                createDefault: () => new SampleState { Count = 0 },
                validate: state => state.Count < 0 ? SaveValidationResult.Invalid("SAMPLE_COUNT_NEGATIVE") : SaveValidationResult.Valid(),
                migrations: new ISaveMigration[] { new IdentityMigration() },
                legacyPlayerPrefsKeys: Array.Empty<string>(),
                codec: new PlainJsonSaveCodec(),
                protector: new Sha256SaveProtector(),
                failurePolicy: SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);

            SaveResult save = service.Save(definition, new SampleState { Count = -1 });

            Assert.IsFalse(save.Success);
            Assert.IsFalse(File.Exists(Path.Combine(tempRoot, "primary", "validation-reject-slot.save")));
        }

        [Test]
        public void Load_MultiStepMigrationChain_AppliesEachStepInOrder()
        {
            var store = new FileSaveStore(tempRoot, new SystemFileSaveOperations());
            var v3Definition = new SaveDefinition<SampleState>(
                key: "migration-chain-slot",
                currentSchemaVersion: 3,
                createDefault: () => new SampleState { Count = -1 },
                validate: _ => SaveValidationResult.Valid(),
                migrations: new ISaveMigration[]
                {
                    new IncrementMigration(1, 2),
                    new IncrementMigration(2, 3),
                },
                legacyPlayerPrefsKeys: Array.Empty<string>(),
                codec: new PlainJsonSaveCodec(),
                protector: new Sha256SaveProtector(),
                failurePolicy: SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);

            // Seed a v1 envelope directly, bypassing Save (which always writes at current schema).
            var v1Service = new PersistenceService(store, null);
            var v1Definition = new SaveDefinition<SampleState>(
                key: "migration-chain-slot",
                currentSchemaVersion: 1,
                createDefault: () => new SampleState { Count = 0 },
                validate: _ => SaveValidationResult.Valid(),
                migrations: new ISaveMigration[] { new IdentityMigration() },
                legacyPlayerPrefsKeys: Array.Empty<string>(),
                codec: new PlainJsonSaveCodec(),
                protector: new Sha256SaveProtector(),
                failurePolicy: SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);
            Assert.IsTrue(v1Service.Save(v1Definition, new SampleState { Count = 1 }).Success);

            var v3Service = new PersistenceService(store, null);
            LoadResult<SampleState> loaded = v3Service.Load(v3Definition);

            Assert.IsTrue(loaded.Success);
            Assert.AreEqual(3, loaded.Value.Count, "Both chained migrations (1->2, 2->3) must run in order, starting from Count=1.");

            // A schema-version mismatch on load re-persists the migrated value at the current
            // schema, so a second load must see it already at v3 with no further migration needed.
            LoadResult<SampleState> reloaded = v3Service.Load(v3Definition);
            Assert.IsTrue(reloaded.Success);
            Assert.AreEqual(3, reloaded.Value.Count);
        }

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

        private sealed class IncrementMigration : ISaveMigration
        {
            public IncrementMigration(int from, int to)
            {
                FromVersion = from;
                ToVersion = to;
            }

            public int FromVersion { get; }
            public int ToVersion { get; }

            public JObject Migrate(JObject source)
            {
                int count = source["Count"]?.Value<int>() ?? 0;
                source["Count"] = count + 1;
                return source;
            }
        }
    }
}

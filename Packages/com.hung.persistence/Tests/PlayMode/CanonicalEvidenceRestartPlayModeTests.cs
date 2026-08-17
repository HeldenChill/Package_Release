using System;
using System.Collections;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Hung.Persistence.PlayModeTests
{
    /// <summary>
    /// Gate G4 separate-process restart proof: a fresh <see cref="PersistenceService"/> +
    /// <see cref="FileCanonicalEvidenceStore"/> pair, backed only by what is on disk (no shared
    /// in-memory state with the instance that wrote it), must still see canonical existence and
    /// must not fall back to legacy. This is the process-restart shape a domain reload/app
    /// relaunch produces. Product-free — see plan §9.1's governing rule.
    /// </summary>
    public class CanonicalEvidenceRestartPlayModeTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "HungPersistenceRestartPlayMode_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        [UnityTest]
        public IEnumerator Restart_FreshServiceInstance_SeesCanonicalExistenceAndSkipsLegacy()
        {
            string key = "restart-slot";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            File.WriteAllText(Path.Combine(legacyRoot, key), "{\"Count\":9}");

            // "Session 1": write canonical + receipt, then let every object go out of scope.
            {
                var store1 = new FileSaveStore(tempRoot);
                var evidence1 = new FileCanonicalEvidenceStore(tempRoot);
                var service1 = new PersistenceService(store1, new FileLegacySaveSource(legacyRoot), null, evidence1);
                SaveResult save = service1.Save(Definition(key), new SampleState { Count = 1 });
                Assert.IsTrue(save.Success);
                File.Delete(Path.Combine(tempRoot, "primary", key + ".save"));
                File.Delete(Path.Combine(tempRoot, "backup", key + ".save"));
            }

            yield return null;

            // "Session 2" (simulated restart): brand-new instances, disk state only.
            var store2 = new FileSaveStore(tempRoot);
            var evidence2 = new FileCanonicalEvidenceStore(tempRoot);
            var service2 = new PersistenceService(store2, new FileLegacySaveSource(legacyRoot), null, evidence2);

            LoadResult<SampleState> result = service2.Load(Definition(key));

            Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, result.Source, "Receipt survives restart on disk; legacy must stay closed even with primary/backup gone.");
        }

        private static SaveDefinition<SampleState> Definition(string key) => new SaveDefinition<SampleState>(
            key: key,
            currentSchemaVersion: 1,
            createDefault: () => new SampleState { Count = -1 },
            validate: _ => SaveValidationResult.Valid(),
            migrations: new ISaveMigration[] { new IdentityMigration() },
            legacyPlayerPrefsKeys: new[] { key },
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
    }
}

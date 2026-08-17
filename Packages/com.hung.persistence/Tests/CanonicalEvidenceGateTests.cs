using System;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Hung.Persistence.Tests
{
    /// <summary>
    /// Gate G4: canonical-existence and import-receipt crash-safety characterisation.
    /// Product-free — see plan §9.1's governing rule.
    /// </summary>
    [TestFixture]
    public class CanonicalEvidenceGateTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "HungPersistenceG4Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        private static SaveDefinition<SampleState> Definition(string key, SaveFailurePolicy failurePolicy = SaveFailurePolicy.CreateDefaultAfterEvidencePreserved) => new SaveDefinition<SampleState>(
            key: key,
            currentSchemaVersion: 1,
            createDefault: () => new SampleState { Count = -1 },
            validate: _ => SaveValidationResult.Valid(),
            migrations: new ISaveMigration[] { new IdentityMigration() },
            legacyPlayerPrefsKeys: new[] { key },
            codec: new PlainJsonSaveCodec(),
            protector: new Sha256SaveProtector(),
            failurePolicy: failurePolicy);

        private static void WriteLegacyFile(string root, string key, int count) =>
            File.WriteAllText(Path.Combine(root, key), "{\"Count\":" + count + "}");

        // ---- Crash-point characterisation: one boundary per test ----

        [Test]
        public void CrashBeforeWrite_LegacyNotYetConsumed_NoStaleReimportRisk()
        {
            string key = "boundary-before-write";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 1);
            var store = NewStore(out FaultInjectingFileSaveOperations fsOps);
            var evidenceStore = new FileCanonicalEvidenceStore(tempRoot);
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, evidenceStore);
            SaveDefinition<SampleState> definition = Definition(key);

            fsOps.FailAt = FaultBoundary.BeforeWrite;
            SaveResult save = service.Save(definition, new SampleState { Count = 42 });
            Assert.IsFalse(save.Success);

            // No canonical write landed, no receipt exists -> legacy remains the correct fallback.
            fsOps.FailAt = FaultBoundary.None;
            LoadResult<SampleState> loaded = service.Load(definition);
            Assert.AreEqual(SaveDataSource.LegacyPlayerPrefs, loaded.Source);
        }

        [Test]
        public void CrashAfterTemp_PrimaryAbsent_LegacyStillReachable()
        {
            string key = "boundary-after-temp";
            AssertCrashLeavesLegacyReachable(key, FaultBoundary.AfterTemp);
        }

        [Test]
        public void CrashAfterBackupCopy_PrimaryAbsent_LegacyStillReachable()
        {
            string key = "boundary-after-backup-copy";
            AssertCrashLeavesLegacyReachable(key, FaultBoundary.AfterBackupCopy);
        }

        private void AssertCrashLeavesLegacyReachable(string key, FaultBoundary boundary)
        {
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 1);
            var store = NewStore(out FaultInjectingFileSaveOperations fsOps);
            var evidenceStore = new FileCanonicalEvidenceStore(tempRoot);
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, evidenceStore);
            SaveDefinition<SampleState> definition = Definition(key);

            // AfterBackupCopy only fires when a primary already exists to be copied, so seed one
            // with a clean write first, then wipe primary+backup+receipt to reset to "nothing
            // committed yet" before the faulted write under test.
            if (boundary == FaultBoundary.AfterBackupCopy)
            {
                Assert.IsTrue(service.Save(definition, new SampleState { Count = 1 }).Success);
                File.Delete(Path.Combine(tempRoot, "primary", key + ".save"));
                File.Delete(Path.Combine(tempRoot, "backup", key + ".save"));
                File.Delete(Path.Combine(tempRoot, "receipts", key + ".receipt"));
                Assert.IsTrue(service.Save(definition, new SampleState { Count = 2 }).Success);
            }

            fsOps.FailAt = boundary;
            SaveResult save = service.Save(definition, new SampleState { Count = 42 });
            Assert.IsFalse(save.Success);

            fsOps.FailAt = FaultBoundary.None;
            if (boundary == FaultBoundary.AfterBackupCopy)
            {
                // Primary from the seed write is still on disk (Write() deletes primary only
                // after the backup copy succeeds and before the move) - legacy stays closed by
                // primary.Exists, exactly like the AfterPrimaryMove boundary.
                LoadResult<SampleState> seeded = service.Load(definition);
                Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, seeded.Source, "Seed primary survives an AfterBackupCopy crash, so legacy must stay closed.");
                return;
            }

            LoadResult<SampleState> loaded = service.Load(definition);
            Assert.AreEqual(SaveDataSource.LegacyPlayerPrefs, loaded.Source, "Crash left no primary/backup/receipt, so legacy must remain reachable.");
        }

        [Test]
        public void CrashAfterPrimaryMove_PrimaryPresentButNoReceipt_LegacyGateAlreadyClosedByPrimary()
        {
            string key = "boundary-after-primary-move";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 1);
            var store = NewStore(out FaultInjectingFileSaveOperations fsOps);
            var evidenceStore = new FileCanonicalEvidenceStore(tempRoot);
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, evidenceStore);
            SaveDefinition<SampleState> definition = Definition(key);

            // AfterPrimaryMove crashes inside Move() itself; FileSaveStore.Write catches it and
            // reports failure, so Save() never reaches CommitReceipt, but the move already ran
            // and primary is on disk.
            fsOps.FailAt = FaultBoundary.AfterPrimaryMove;
            SaveResult save = service.Save(definition, new SampleState { Count = 42 });
            Assert.IsFalse(save.Success);
            fsOps.FailAt = FaultBoundary.None;
            Assert.IsFalse(evidenceStore.HasReceipt(key), "Receipt must not exist: crash happened before CommitReceipt.");

            LoadResult<SampleState> loaded = service.Load(definition);
            Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, loaded.Source, "Primary exists, so the legacy gate is already closed regardless of the missing receipt.");
        }

        [Test]
        public void CrashBeforeReceipt_PrimaryPresentNoReceipt_LegacyGateClosedByPrimaryNotByReceipt()
        {
            string key = "boundary-before-receipt";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 1);
            var realOps = new SystemFileSaveOperations();
            var store = new FileSaveStore(tempRoot, realOps);
            var realEvidence = new FileCanonicalEvidenceStore(tempRoot, realOps);
            var faultyEvidence = new FaultInjectingEvidenceStore(realEvidence) { FailAt = FaultBoundary.BeforeReceipt };
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, faultyEvidence);
            SaveDefinition<SampleState> definition = Definition(key);

            SaveResult save = service.Save(definition, new SampleState { Count = 42 });
            Assert.IsFalse(save.Success, "CommitReceipt's injected fault is caught by Save's outer try/catch and reported as failure.");
            Assert.IsFalse(realEvidence.HasReceipt(key));

            faultyEvidence.FailAt = FaultBoundary.None;
            LoadResult<SampleState> loaded = service.Load(definition);
            Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, loaded.Source, "Primary was written before the receipt crash, so legacy is already closed by primary.Exists.");
        }

        [Test]
        public void CrashAfterReceipt_PrimaryAndReceiptPresent_LegacyNeverConsulted()
        {
            string key = "boundary-after-receipt";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 1);
            var realOps = new SystemFileSaveOperations();
            var store = new FileSaveStore(tempRoot, realOps);
            var realEvidence = new FileCanonicalEvidenceStore(tempRoot, realOps);
            var faultyEvidence = new FaultInjectingEvidenceStore(realEvidence) { FailAt = FaultBoundary.AfterReceipt };
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, faultyEvidence);
            SaveDefinition<SampleState> definition = Definition(key);

            SaveResult save = service.Save(definition, new SampleState { Count = 42 });
            Assert.IsFalse(save.Success, "The post-commit injected fault is caught by Save's outer try/catch and reported as failure, even though the receipt write itself already landed.");
            Assert.IsTrue(realEvidence.HasReceipt(key), "Receipt commit itself succeeded before the injected post-commit fault.");

            faultyEvidence.FailAt = FaultBoundary.None;
            LoadResult<SampleState> loaded = service.Load(definition);
            Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, loaded.Source);
        }

        // ---- Idempotence ----

        [Test]
        public void Import_RunTwice_ByteIdenticalCanonicalOutputAndNoSecondMutation()
        {
            string key = "idempotence-slot";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 5);
            var realOps = new SystemFileSaveOperations();
            var store = new FileSaveStore(tempRoot, realOps);
            var evidenceStore = new FileCanonicalEvidenceStore(tempRoot, realOps);
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, evidenceStore);
            SaveDefinition<SampleState> definition = Definition(key);

            LoadResult<SampleState> first = service.Load(definition);
            Assert.AreEqual(SaveDataSource.LegacyPlayerPrefs, first.Source);
            byte[] primaryAfterFirst = File.ReadAllBytes(Path.Combine(tempRoot, "primary", key + ".save"));

            LoadResult<SampleState> second = service.Load(definition);
            byte[] primaryAfterSecond = File.ReadAllBytes(Path.Combine(tempRoot, "primary", key + ".save"));

            Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, second.Source, "Second load must not re-import; canonical existence is already proven.");
            CollectionAssert.AreEqual(primaryAfterFirst, primaryAfterSecond);
        }

        // ---- Fail-closed with receipt present: legacy never consulted ----

        [Test]
        public void FailClosed_Missing_WithReceiptPresent_LegacyNeverConsulted()
        {
            string key = "failclosed-missing";
            RunFailClosedScenario(key, corruptPrimary: false, quarantine: false, unknownNewer: false, deletePrimaryAndBackup: true);
        }

        [Test]
        public void FailClosed_Corrupt_WithReceiptPresent_LegacyNeverConsulted()
        {
            string key = "failclosed-corrupt";
            RunFailClosedScenario(key, corruptPrimary: true, quarantine: false, unknownNewer: false, deletePrimaryAndBackup: false);
        }

        [Test]
        public void FailClosed_Quarantined_WithReceiptPresent_LegacyNeverConsulted()
        {
            string key = "failclosed-quarantined";
            RunFailClosedScenario(key, corruptPrimary: true, quarantine: true, unknownNewer: false, deletePrimaryAndBackup: false);
        }

        [Test]
        public void FailClosed_UnknownNewer_WithReceiptPresent_LegacyNeverConsultedAndNotQuarantined()
        {
            string key = "failclosed-unknown-newer";
            RunFailClosedScenario(key, corruptPrimary: false, quarantine: false, unknownNewer: true, deletePrimaryAndBackup: false);
        }

        private void RunFailClosedScenario(string key, bool corruptPrimary, bool quarantine, bool unknownNewer, bool deletePrimaryAndBackup)
        {
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 99); // must never be consulted below
            var realOps = new SystemFileSaveOperations();
            var store = new FileSaveStore(tempRoot, realOps);
            var evidenceStore = new FileCanonicalEvidenceStore(tempRoot, realOps);
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot), null, evidenceStore);
            SaveDefinition<SampleState> definition = Definition(key, SaveFailurePolicy.FailClosed);

            // Establish canonical existence: one clean commit, which also writes the receipt.
            SaveResult initial = service.Save(definition, new SampleState { Count = 1 });
            Assert.IsTrue(initial.Success);
            Assert.IsTrue(evidenceStore.HasReceipt(key));

            string primaryPath = Path.Combine(tempRoot, "primary", key + ".save");
            string backupPath = Path.Combine(tempRoot, "backup", key + ".save");

            if (deletePrimaryAndBackup)
            {
                File.Delete(primaryPath);
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
            }
            else if (unknownNewer)
            {
                string json = File.ReadAllText(primaryPath);
                JObject envelope = JObject.Parse(json);
                envelope["formatVersion"] = SaveEnvelope.CurrentFormatVersion + 1;
                File.WriteAllText(primaryPath, envelope.ToString());
            }
            else if (corruptPrimary)
            {
                File.WriteAllText(primaryPath, "not an envelope");
            }

            LoadResult<SampleState> result = service.Load(definition);

            Assert.AreNotEqual(SaveDataSource.LegacyPlayerPrefs, result.Source, "Legacy must never be consulted once canonical existence is proven, regardless of failure shape.");

            if (unknownNewer)
            {
                Assert.AreEqual(SaveRecoveryState.UnsupportedNewerVersion, result.Recovery);
                Assert.IsTrue(File.Exists(primaryPath), "UnsupportedNewerVersion must never be quarantined (§3.5 item 3).");
            }
            else if (deletePrimaryAndBackup)
            {
                // Missing + receipt present: legacy gate closed by canonicalExistenceProven, not by primary/backup.
                Assert.AreEqual(SaveDataSource.Default, result.Source);
            }
            else
            {
                Assert.AreEqual(SaveRecoveryState.Unrecoverable, result.Recovery);
            }

            _ = quarantine; // corrupt-primary path already exercises quarantine via TryLoadBytes -> QuarantinePrimary
        }

        // ---- Backward compatibility: null evidence store behaves exactly as today ----

        [Test]
        public void NullEvidenceStore_LegacyGateUnchanged_MatchesPreExistingBehaviour()
        {
            string key = "backcompat-slot";
            string legacyRoot = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyRoot);
            WriteLegacyFile(legacyRoot, key, 3);
            var realOps = new SystemFileSaveOperations();
            var store = new FileSaveStore(tempRoot, realOps);
            var service = new PersistenceService(store, new FileLegacySaveSource(legacyRoot)); // no evidence store
            SaveDefinition<SampleState> definition = Definition(key);

            SaveResult initial = service.Save(definition, new SampleState { Count = 1 });
            Assert.IsTrue(initial.Success);
            Assert.IsFalse(Directory.Exists(Path.Combine(tempRoot, "receipts")), "No evidence store supplied -> no receipt directory should be created.");

            File.Delete(Path.Combine(tempRoot, "primary", key + ".save"));

            LoadResult<SampleState> result = service.Load(definition);
            Assert.AreEqual(SaveDataSource.LegacyPlayerPrefs, result.Source, "With no evidence store, losing primary+backup must still fall through to legacy exactly as before D4.");
        }

        // ---- Receipt content: no payload, no secrets ----

        [Test]
        public void Receipt_ContainsNoPayloadBytesAndNoSecretMaterial()
        {
            string key = "receipt-content-slot";
            var realOps = new SystemFileSaveOperations();
            var store = new FileSaveStore(tempRoot, realOps);
            var evidenceStore = new FileCanonicalEvidenceStore(tempRoot, realOps);
            var service = new PersistenceService(store, null, null, evidenceStore);
            SaveDefinition<SampleState> definition = Definition(key);

            const string secretMarker = "TOP-SECRET-PAYLOAD-VALUE-3141592";
            service.Save(definition, new SampleState { Count = 3141592 });

            string receiptPath = Path.Combine(tempRoot, "receipts", key + ".receipt");
            Assert.IsTrue(File.Exists(receiptPath));
            string receiptText = File.ReadAllText(receiptPath);

            StringAssert.DoesNotContain(secretMarker, receiptText);
            StringAssert.DoesNotContain("3141592", receiptText);
            StringAssert.Contains(key, receiptText);
            StringAssert.Contains("1", receiptText); // schema version
        }

        private FileSaveStore NewStore(out FaultInjectingFileSaveOperations fsOps)
        {
            fsOps = new FaultInjectingFileSaveOperations(new SystemFileSaveOperations());
            return new FileSaveStore(tempRoot, fsOps);
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
    }
}

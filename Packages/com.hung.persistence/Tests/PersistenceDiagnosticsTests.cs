using System;
using System.Linq;
using System.Text;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    /// <summary>
    /// BUG-261114-f7ae: every non-clean outcome reaches <see cref="ISaveDiagnostics"/>, clean
    /// outcomes stay silent, and a throwing sink never changes a result.
    /// </summary>
    public class PersistenceDiagnosticsTests
    {
        private sealed class State
        {
            public int Value;
        }

        private sealed class ThrowingDiagnostics : ISaveDiagnostics
        {
            public void Report(SaveDiagnostic diagnostic) => throw new InvalidOperationException("sink broke");
        }

        private InMemorySaveStore store;
        private DiagnosticsCollector sink;

        [SetUp]
        public void SetUp()
        {
            store = new InMemorySaveStore();
            sink = new DiagnosticsCollector();
        }

        private PersistenceService Service(ILegacySaveSource legacy = null) => new PersistenceService(store, legacy, sink);

        [Test]
        public void CleanSaveAndLoad_ReportNothing()
        {
            PersistenceService service = Service();
            SaveDefinition<State> definition = Definition(SaveFailurePolicy.FailClosed);
            service.Save(definition, new State { Value = 1 });

            service.Load(definition);

            Assert.That(sink.Events, Is.Empty);
        }

        [Test]
        public void CorruptPrimary_ReportsQuarantineThenDefault()
        {
            store.Primary["diag"] = Encoding.UTF8.GetBytes("not json");

            Service().Load(Definition(SaveFailurePolicy.CreateDefaultAfterEvidencePreserved));

            AssertEvent(0, "SAVE_PRIMARY_CORRUPT", SaveDataSource.Primary, SaveRecoveryState.PrimaryQuarantined);
            Assert.That(sink.Events[0].EvidencePath, Is.Not.Null);
            AssertEvent(1, "SAVE_DEFAULT_CREATED", SaveDataSource.Default, SaveRecoveryState.DefaultCreated);
            Assert.That(sink.Events.Count, Is.EqualTo(2));
        }

        [Test]
        public void CorruptBackup_ReportsBackupQuarantine()
        {
            store.Backup["diag"] = Encoding.UTF8.GetBytes("bad");

            Service().Load(Definition(SaveFailurePolicy.CreateDefaultAfterEvidencePreserved));

            AssertEvent(0, "SAVE_PRIMARY_CORRUPT", SaveDataSource.Backup, SaveRecoveryState.PrimaryQuarantined);
        }

        [Test]
        public void BackupRestored_Reported()
        {
            PersistenceService service = Service();
            SaveDefinition<State> definition = Definition(SaveFailurePolicy.FailClosed);
            service.Save(definition, new State { Value = 3 });
            store.Backup["diag"] = store.Primary["diag"];
            store.Primary["diag"] = Encoding.UTF8.GetBytes("bad");
            sink.Events.Clear();

            service.Load(definition);

            Assert.That(sink.Events.Last().Code, Is.EqualTo("SAVE_BACKUP_RESTORED"));
            Assert.That(sink.Events.Last().Source, Is.EqualTo(SaveDataSource.Backup));
            Assert.That(sink.Events.Last().Recovery, Is.EqualTo(SaveRecoveryState.BackupRestored));
        }

        [Test]
        public void DefaultCreated_Reported()
        {
            Service().Load(Definition(SaveFailurePolicy.CreateDefaultAfterEvidencePreserved));

            AssertEvent(0, "SAVE_DEFAULT_CREATED", SaveDataSource.Default, SaveRecoveryState.DefaultCreated);
            Assert.That(sink.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void LegacyImported_Reported()
        {
            var legacy = new DictionaryLegacySaveSource();
            legacy.Set("diag-legacy", "{\"Value\":4}");

            Service(legacy).Load(Definition(SaveFailurePolicy.FailClosed, legacyKey: "diag-legacy"));

            AssertEvent(0, "SAVE_LEGACY_IMPORTED", SaveDataSource.LegacyPlayerPrefs, SaveRecoveryState.Migrated);
        }

        [Test]
        public void ReadFailed_Reported()
        {
            store.FailPrimaryRead = true;

            Service().Load(Definition(SaveFailurePolicy.FailClosed));

            AssertEvent(0, "SAVE_READ_FAILED", SaveDataSource.Primary, SaveRecoveryState.Unrecoverable);
        }

        [Test]
        public void SaveNewerThanClient_Reported()
        {
            PersistenceService service = Service();
            service.Save(Definition(SaveFailurePolicy.FailClosed, schemaVersion: 2), new State { Value = 5 });

            service.Load(Definition(SaveFailurePolicy.FailClosed));

            AssertEvent(0, "SAVE_SCHEMA_NEWER_THAN_CLIENT", SaveDataSource.Primary, SaveRecoveryState.UnsupportedNewerVersion);
        }

        [Test]
        public void FailClosedCorrupt_ReportsQuarantineThenUnrecoverable()
        {
            store.Primary["diag"] = Encoding.UTF8.GetBytes("not json");

            Service().Load(Definition(SaveFailurePolicy.FailClosed));

            AssertEvent(0, "SAVE_PRIMARY_CORRUPT", SaveDataSource.Primary, SaveRecoveryState.PrimaryQuarantined);
            AssertEvent(1, "SAVE_PRIMARY_CORRUPT", SaveDataSource.None, SaveRecoveryState.Unrecoverable);
        }

        [Test]
        public void SaveWriteFailed_Reported()
        {
            store.FailNextWrite = true;

            Service().Save(Definition(SaveFailurePolicy.FailClosed), new State());

            AssertEvent(0, "SAVE_WRITE_FAILED_PREVIOUS_RETAINED", SaveDataSource.None, SaveRecoveryState.None);
        }

        [Test]
        public void SaveValidationFailed_Reported()
        {
            var definition = new SaveDefinition<State>("diag", 1, () => new State(),
                _ => SaveValidationResult.Invalid("diag-invalid", "rejected"), Array.Empty<ISaveMigration>(),
                Array.Empty<string>(), PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector(),
                SaveFailurePolicy.FailClosed);

            Service().Save(definition, new State());

            AssertEvent(0, "diag-invalid", SaveDataSource.None, SaveRecoveryState.None);
        }

        [Test]
        public void ThrowingSink_DoesNotChangeResults()
        {
            store.Primary["diag"] = Encoding.UTF8.GetBytes("not json");
            var service = new PersistenceService(store, null, new ThrowingDiagnostics());
            SaveDefinition<State> definition = Definition(SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);

            LoadResult<State> loaded = service.Load(definition);
            store.FailNextWrite = true;
            SaveResult saved = service.Save(definition, new State());

            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Recovery, Is.EqualTo(SaveRecoveryState.DefaultCreated));
            Assert.That(saved.Success, Is.False);
            Assert.That(saved.DiagnosticCode, Is.EqualTo("SAVE_WRITE_FAILED_PREVIOUS_RETAINED"));
        }

        private void AssertEvent(int index, string code, SaveDataSource source, SaveRecoveryState recovery)
        {
            Assert.That(sink.Events.Count, Is.GreaterThan(index), "Expected diagnostic " + index + " was not reported.");
            SaveDiagnostic diagnostic = sink.Events[index];
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.SaveKey, Is.EqualTo("diag"));
            Assert.That(diagnostic.Source, Is.EqualTo(source));
            Assert.That(diagnostic.Recovery, Is.EqualTo(recovery));
            Assert.That(diagnostic.Message, Is.Null);
        }

        private static SaveDefinition<State> Definition(SaveFailurePolicy policy, int schemaVersion = 1, string legacyKey = null)
        {
            return new SaveDefinition<State>(
                "diag",
                schemaVersion,
                () => new State(),
                _ => SaveValidationResult.Valid(),
                new ISaveMigration[] { new LegacyRawJsonToSchemaOneMigration() },
                legacyKey == null ? Array.Empty<string>() : new[] { legacyKey },
                PersistenceTestDoubles.Codec(),
                PersistenceTestDoubles.Protector(),
                policy);
        }
    }
}

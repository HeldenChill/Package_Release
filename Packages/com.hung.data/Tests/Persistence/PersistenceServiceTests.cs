using System;
using System.Text;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class PersistenceServiceTests
    {
        private sealed class ValuableState
        {
            public int Value;
        }

        [Test]
        public void Service_SaveLoad_RoundTripsThroughEnvelope()
        {
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store);
            SaveDefinition<ValuableState> definition = Definition(SaveFailurePolicy.FailClosed);

            Assert.That(service.Save(definition, new ValuableState { Value = 7 }).Success, Is.True);
            LoadResult<ValuableState> loaded = service.Load(definition);

            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Source, Is.EqualTo(SaveDataSource.Primary));
            Assert.That(loaded.Value.Value, Is.EqualTo(7));
        }

        [Test]
        public void Service_CorruptFailClosed_DoesNotOverwriteEvidence()
        {
            var store = new InMemorySaveStore();
            store.Primary["valuable"] = Encoding.UTF8.GetBytes("not json");
            var service = new PersistenceService(store);

            LoadResult<ValuableState> result = service.Load(Definition(SaveFailurePolicy.FailClosed));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
            Assert.That(store.WriteCount, Is.EqualTo(0));
            Assert.That(store.QuarantinedBytes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Service_CorruptLowValue_CreatesDefaultAfterQuarantine()
        {
            var store = new InMemorySaveStore();
            store.Primary["valuable"] = Encoding.UTF8.GetBytes("not json");
            var service = new PersistenceService(store);

            LoadResult<ValuableState> result = service.Load(Definition(SaveFailurePolicy.CreateDefaultAfterEvidencePreserved));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.DefaultCreated));
            Assert.That(store.QuarantinedBytes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Service_ValidBackupRestoresAfterCorruptPrimary()
        {
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store);
            SaveDefinition<ValuableState> definition = Definition(SaveFailurePolicy.FailClosed);
            service.Save(definition, new ValuableState { Value = 3 });
            store.Backup["valuable"] = store.Primary["valuable"];
            store.Primary["valuable"] = Encoding.UTF8.GetBytes("bad");

            LoadResult<ValuableState> result = service.Load(definition);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.BackupRestored));
            Assert.That(result.Value.Value, Is.EqualTo(3));
        }

        private static SaveDefinition<ValuableState> Definition(SaveFailurePolicy policy)
        {
            return new SaveDefinition<ValuableState>(
                "valuable",
                1,
                () => new ValuableState(),
                _ => SaveValidationResult.Valid(),
                Array.Empty<ISaveMigration>(),
                Array.Empty<string>(),
                PersistenceTestDoubles.Codec(),
                PersistenceTestDoubles.Protector(),
                policy);
        }
    }
}

using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Persistence.Tests.DataComposition
{
    public sealed class MigrationCommitTests
    {
        [Test] public void FailedLegacyImportIsNotReportedAsDurableAndRetryRetainsReceipt()
        {
            var store = new MemoryStore { FailWrites = true };
            var legacy = new Legacy();
            string original = legacy.Json;
            var definition = Definitions.Make<Wallet>("wallet", new[] { "old-wallet" });
            var failed = new PersistenceService(store, legacy).Load(definition);
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.DiagnosticCode, Is.EqualTo("SAVE_WRITE_FAILED_PREVIOUS_RETAINED"));
            Assert.That(failed.Source, Is.EqualTo(SaveDataSource.LegacyPlayerPrefs));
            Assert.That(failed.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
            Assert.That(store.Primary.Count, Is.Zero);
            Assert.That(legacy.Json, Is.EqualTo(original));
            store.FailWrites = false;
            var retried = new PersistenceService(store, legacy).Load(definition);
            Assert.That(retried.Success, Is.True);
            Assert.That(retried.Value.Balance, Is.EqualTo(7));
            Assert.That(retried.Value.Receipts, Is.EqualTo(new[] { "claim.old" }));
            var restart = new PersistenceService(store).Load(definition);
            Assert.That(restart.Success, Is.True);
            Assert.That(restart.Source, Is.EqualTo(SaveDataSource.Primary));
            Assert.That(restart.Value.Receipts, Is.EqualTo(new[] { "claim.old" }));
        }
        [Test] public void FailedPrimarySchemaRewritePreservesOriginalBytesAndStopsRecovery()
        {
            var store = new MemoryStore();
            var one = Definitions.Make<Wallet>("wallet");
            Assert.That(new PersistenceService(store).Save(one, new Wallet { Balance = 9, Receipts = { "claim.one" } }).Success, Is.True);
            byte[] original = (byte[])store.Primary["wallet"].Clone();
            var two = Definitions.Make<Wallet>("wallet", schema: 2);
            store.FailWrites = true;
            var failed = new PersistenceService(store).Load(two);
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.DiagnosticCode, Is.EqualTo("SAVE_WRITE_FAILED_PREVIOUS_RETAINED"));
            Assert.That(failed.Source, Is.EqualTo(SaveDataSource.Primary));
            Assert.That(failed.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
            Assert.That(store.Quarantines, Is.Zero);
            CollectionAssert.AreEqual(original, store.Primary["wallet"]);
            store.FailWrites = false;
            Assert.That(new PersistenceService(store).Load(two).Success, Is.True);
            var restarted = new PersistenceService(store).Load(two);
            Assert.That(restarted.Value.Balance, Is.EqualTo(9));
            Assert.That(restarted.Value.Receipts, Is.EqualTo(new[] { "claim.one" }));
        }
        [Test] public void FailedBackupSchemaRewritePreservesBackupAndDoesNotCreateDefaults()
        {
            var store = new MemoryStore();
            var one = Definitions.Make<Wallet>("wallet");
            Assert.That(new PersistenceService(store).Save(one, new Wallet { Balance = 11 }).Success, Is.True);
            store.Backup["wallet"] = (byte[])store.Primary["wallet"].Clone();
            store.Primary.Clear();
            byte[] original = (byte[])store.Backup["wallet"].Clone();
            store.FailWrites = true;
            var failed = new PersistenceService(store).Load(Definitions.Make<Wallet>("wallet", schema: 2));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Source, Is.EqualTo(SaveDataSource.Backup));
            Assert.That(failed.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
            Assert.That(store.Quarantines, Is.Zero);
            CollectionAssert.AreEqual(original, store.Backup["wallet"]);
            // Recovery restores the validated old-schema source before attempting its rewrite.
            CollectionAssert.AreEqual(original, store.Primary["wallet"]);
        }
        [Test] public void NewerSchemaRemainsUntouchedAndFailsClosed()
        {
            var store = new MemoryStore();
            Assert.That(new PersistenceService(store).Save(Definitions.Make<Wallet>("wallet", schema: 2), new Wallet { Balance = 5 }).Success, Is.True);
            byte[] original = (byte[])store.Primary["wallet"].Clone();
            var result = new PersistenceService(store).Load(Definitions.Make<Wallet>("wallet"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.UnsupportedNewerVersion));
            Assert.That(store.Quarantines, Is.Zero);
            CollectionAssert.AreEqual(original, store.Primary["wallet"]);
        }
    }
}

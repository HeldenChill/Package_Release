using System;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Persistence.Tests.DataComposition
{
    public sealed class BackupMigrationRecoveryTests
    {
        [Test] public void CorruptPrimaryAndOlderBackupMigrateDurablyWithoutRotatingCorruption()
        {
            WithRoot(root =>
            {
                Seed(root);
                File.WriteAllText(Primary(root), "CORRUPT_PRIMARY");
                byte[] originalBackup = File.ReadAllBytes(Backup(root));
                var definition = Definitions.Make<Wallet>("wallet", schema: 2);
                var loaded = new PersistenceService(new FileSaveStore(root)).Load(definition);
                RequireRecovered(loaded);
                CollectionAssert.AreEqual(originalBackup, File.ReadAllBytes(Backup(root)));
                Assert.That(SaveEnvelope.FromJson(File.ReadAllText(Primary(root))).PayloadSchemaVersion, Is.EqualTo(2));
                RequireRecovered(new PersistenceService(new FileSaveStore(root)).Load(definition));
                Assert.That(Directory.GetFiles(Path.Combine(root, "quarantine")).Length, Is.GreaterThanOrEqualTo(1));
            });
        }

        [Test] public void MissingPrimaryAndOlderBackupKeepMigratedSchemaAfterRecovery()
        {
            WithRoot(root =>
            {
                Seed(root);
                File.Delete(Primary(root));
                var definition = Definitions.Make<Wallet>("wallet", schema: 2);
                RequireRecovered(new PersistenceService(new FileSaveStore(root)).Load(definition));
                Assert.That(SaveEnvelope.FromJson(File.ReadAllText(Primary(root))).PayloadSchemaVersion, Is.EqualTo(2));
                RequireRecovered(new PersistenceService(new FileSaveStore(root)).Load(definition));
            });
        }

        [Test] public void FailedBackupRestoreDoesNotPublishMigratedSuccessOrWriteOverBackup()
        {
            WithRoot(root =>
            {
                Seed(root);
                File.WriteAllText(Primary(root), "CORRUPT_PRIMARY");
                byte[] originalBackup = File.ReadAllBytes(Backup(root));
                var operations = new RecoveryFaults { FailRestore = true };
                var definition = Definitions.Make<Wallet>("wallet", schema: 2);
                var failed = new PersistenceService(new FileSaveStore(root, operations)).Load(definition);
                Assert.That(failed.Success, Is.False);
                Assert.That(failed.DiagnosticCode, Is.EqualTo("SAVE_WRITE_FAILED_PREVIOUS_RETAINED"));
                Assert.That(failed.Source, Is.EqualTo(SaveDataSource.Backup));
                Assert.That(failed.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
                CollectionAssert.AreEqual(originalBackup, File.ReadAllBytes(Backup(root)));
                RequireRecovered(new PersistenceService(new FileSaveStore(root)).Load(definition));
            });
        }

        [Test] public void FailedBackupMigrationPromotionRetainsValidBackupForFreshRetry()
        {
            WithRoot(root =>
            {
                Seed(root);
                File.WriteAllText(Primary(root), "CORRUPT_PRIMARY");
                byte[] originalBackup = File.ReadAllBytes(Backup(root));
                var operations = new RecoveryFaults { FailPromotion = true };
                var definition = Definitions.Make<Wallet>("wallet", schema: 2);
                var failed = new PersistenceService(new FileSaveStore(root, operations)).Load(definition);
                Assert.That(failed.Success, Is.False);
                Assert.That(failed.Source, Is.EqualTo(SaveDataSource.Backup));
                Assert.That(failed.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
                CollectionAssert.AreEqual(originalBackup, File.ReadAllBytes(Backup(root)));
                RequireRecovered(new PersistenceService(new FileSaveStore(root)).Load(definition));
            });
        }

        [Test] public void SameSchemaRestoreFailureDoesNotReportBackupRestored()
        {
            WithRoot(root =>
            {
                Seed(root);
                File.Delete(Primary(root));
                byte[] originalBackup = File.ReadAllBytes(Backup(root));
                var failed = new PersistenceService(new FileSaveStore(root, new RecoveryFaults { FailRestore = true }))
                    .Load(Definitions.Make<Wallet>("wallet"));
                Assert.That(failed.Success, Is.False);
                Assert.That(failed.Recovery, Is.EqualTo(SaveRecoveryState.Unrecoverable));
                CollectionAssert.AreEqual(originalBackup, File.ReadAllBytes(Backup(root)));
                Assert.That(File.Exists(Primary(root)), Is.False);
            });
        }

        private static void RequireRecovered(LoadResult<Wallet> loaded)
        {
            Assert.That(loaded.Success, Is.True, loaded.DiagnosticCode);
            Assert.That(loaded.Value.Balance, Is.EqualTo(9));
            Assert.That(loaded.Value.Receipts, Is.EqualTo(new[] { "claim.backup" }));
        }

        private static void Seed(string root)
        {
            var service = new PersistenceService(new FileSaveStore(root));
            var definition = Definitions.Make<Wallet>("wallet");
            Assert.That(service.Save(definition, new Wallet { Balance = 9, Receipts = { "claim.backup" } }).Success, Is.True);
            Assert.That(service.Save(definition, new Wallet { Balance = 11, Receipts = { "claim.latest" } }).Success, Is.True);
        }

        private static string Primary(string root) => Path.Combine(root, "primary", "wallet.save");
        private static string Backup(string root) => Path.Combine(root, "backup", "wallet.save");

        private static void WithRoot(Action<string> action)
        {
            string root = Path.Combine(Path.GetTempPath(), "HungBackupRecovery_" + Guid.NewGuid().ToString("N"));
            try { action(root); }
            finally
            {
                string full = Path.GetFullPath(root);
                string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("HungBackupRecovery_", StringComparison.Ordinal))
                    throw new InvalidOperationException("Refusing cleanup outside the owned test root.");
                if (Directory.Exists(full)) Directory.Delete(full, true);
            }
        }

        private sealed class RecoveryFaults : IFileSaveOperations
        {
            private readonly SystemFileSaveOperations inner = new SystemFileSaveOperations();
            public bool FailRestore;
            public bool FailPromotion;
            public void CreateDirectory(string path) => inner.CreateDirectory(path);
            public bool FileExists(string path) => inner.FileExists(path);
            public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
            public Stream CreateNew(string path) => inner.CreateNew(path);
            public void Copy(string source, string destination, bool overwrite)
            {
                if (FailRestore && Path.GetFileName(Path.GetDirectoryName(source)) == "backup" && Path.GetFileName(Path.GetDirectoryName(destination)) == "primary")
                    throw new IOException("Injected backup restore failure.");
                inner.Copy(source, destination, overwrite);
            }
            public void Move(string source, string destination)
            {
                if (FailPromotion && source.EndsWith(".tmp", StringComparison.Ordinal) && destination.EndsWith(".save", StringComparison.Ordinal))
                    throw new IOException("Injected primary promotion failure.");
                inner.Move(source, destination);
            }
            public void Delete(string path) => inner.Delete(path);
        }
    }
}

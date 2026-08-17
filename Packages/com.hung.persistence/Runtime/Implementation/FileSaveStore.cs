using System;
using System.IO;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string root;
        private readonly string primaryDirectory;
        private readonly string backupDirectory;
        private readonly string quarantineDirectory;
        private readonly IFileSaveOperations operations;

        public FileSaveStore(string root, IFileSaveOperations operations = null)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.operations = operations ?? new SystemFileSaveOperations();
            primaryDirectory = Path.Combine(root, "primary");
            backupDirectory = Path.Combine(root, "backup");
            quarantineDirectory = Path.Combine(root, "quarantine");
        }

        public SaveStoreReadResult ReadPrimary(string key) => Read(PrimaryPath(key));
        public SaveStoreReadResult ReadBackup(string key) => Read(BackupPath(key));

        public SaveStoreWriteResult Write(string key, byte[] content)
        {
            try
            {
                string primary = PrimaryPath(key);
                string backup = BackupPath(key);
                operations.CreateDirectory(primaryDirectory);
                operations.CreateDirectory(backupDirectory);
                string temp = Path.Combine(primaryDirectory, key + "." + Guid.NewGuid().ToString("N") + ".tmp");
                using (Stream stream = operations.CreateNew(temp))
                {
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }

                if (operations.FileExists(primary))
                    operations.Copy(primary, backup, true);
                if (operations.FileExists(primary))
                    operations.Delete(primary);
                operations.Move(temp, primary);
                return SaveStoreWriteResult.Succeeded(primary);
            }
            catch
            {
                return SaveStoreWriteResult.Failed("SAVE_WRITE_FAILED_PREVIOUS_RETAINED");
            }
        }

        public SaveStoreWriteResult RestoreBackup(string key)
        {
            try
            {
                string backup = BackupPath(key);
                if (!operations.FileExists(backup))
                    return SaveStoreWriteResult.Failed("SAVE_BACKUP_MISSING");
                string primary = PrimaryPath(key);
                operations.CreateDirectory(primaryDirectory);
                operations.Copy(backup, primary, true);
                return SaveStoreWriteResult.Succeeded(primary);
            }
            catch
            {
                return SaveStoreWriteResult.Failed("SAVE_WRITE_FAILED_PREVIOUS_RETAINED");
            }
        }

        public SaveStoreWriteResult QuarantinePrimary(string key, byte[] content, string reason) => Quarantine(key, content, reason, "primary");
        public SaveStoreWriteResult QuarantineBackup(string key, byte[] content, string reason) => Quarantine(key, content, reason, "backup");

        private SaveStoreReadResult Read(string path)
        {
            try
            {
                return operations.FileExists(path) ? SaveStoreReadResult.Found(operations.ReadAllBytes(path)) : SaveStoreReadResult.Missing();
            }
            catch
            {
                return SaveStoreReadResult.Failed("SAVE_READ_FAILED");
            }
        }

        private SaveStoreWriteResult Quarantine(string key, byte[] content, string reason, string source)
        {
            try
            {
                SaveDefinition.ValidateKey(key);
                SaveDefinition.ValidateKey(reason);
                operations.CreateDirectory(quarantineDirectory);
                string path = Path.Combine(quarantineDirectory, key + "." + source + "." + reason + "." + DateTime.UtcNow.Ticks + ".save");
                using (Stream stream = operations.CreateNew(path))
                {
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
                return SaveStoreWriteResult.Succeeded(path);
            }
            catch
            {
                return SaveStoreWriteResult.Failed("SAVE_QUARANTINE_FAILED");
            }
        }

        private string PrimaryPath(string key)
        {
            SaveDefinition.ValidateKey(key);
            return Path.Combine(primaryDirectory, key + ".save");
        }

        private string BackupPath(string key)
        {
            SaveDefinition.ValidateKey(key);
            return Path.Combine(backupDirectory, key + ".save");
        }
    }
}

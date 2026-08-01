using System.Collections.Generic;
using Hung.Base.Persistence;

namespace Hung.Data.Tests.Persistence
{
    internal sealed class InMemorySaveStore : ISaveStore
    {
        public readonly Dictionary<string, byte[]> Primary = new Dictionary<string, byte[]>();
        public readonly Dictionary<string, byte[]> Backup = new Dictionary<string, byte[]>();
        public readonly List<byte[]> QuarantinedBytes = new List<byte[]>();
        public int WriteCount { get; private set; }
        public bool FailNextWrite { get; set; }

        public SaveStoreReadResult ReadPrimary(string key) => Primary.TryGetValue(key, out byte[] bytes) ? SaveStoreReadResult.Found(bytes) : SaveStoreReadResult.Missing();
        public SaveStoreReadResult ReadBackup(string key) => Backup.TryGetValue(key, out byte[] bytes) ? SaveStoreReadResult.Found(bytes) : SaveStoreReadResult.Missing();

        public SaveStoreWriteResult Write(string key, byte[] content)
        {
            WriteCount++;
            if (FailNextWrite)
            {
                FailNextWrite = false;
                return SaveStoreWriteResult.Failed("SAVE_WRITE_FAILED_PREVIOUS_RETAINED");
            }

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
            QuarantinedBytes.Add(content);
            Primary.Remove(key);
            return SaveStoreWriteResult.Succeeded(reason);
        }

        public SaveStoreWriteResult QuarantineBackup(string key, byte[] content, string reason)
        {
            QuarantinedBytes.Add(content);
            Backup.Remove(key);
            return SaveStoreWriteResult.Succeeded(reason);
        }
    }
}

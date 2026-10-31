using System;
using System.Collections.Generic;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;

namespace Hung.Persistence.Tests.DataComposition
{
    public sealed class Wallet
    {
        public int Balance;
        public List<string> Receipts = new List<string>();
    }
    public sealed class Preferences { public bool Muted; }
    public sealed class IdentityStep : ISaveMigration
    {
        public int FromVersion { get; }
        public int ToVersion => FromVersion + 1;
        public IdentityStep(int from) { FromVersion = from; }
        public JObject Migrate(JObject source) => source;
    }
    public static class Definitions
    {
        public static SaveDefinition<T> Make<T>(string key, string[] aliases = null, int schema = 1,
            Func<T> defaults = null, SaveFailurePolicy policy = SaveFailurePolicy.FailClosed) where T : new()
        {
            var steps = new List<ISaveMigration>();
            for (int i = 0; i < schema; i++) steps.Add(new IdentityStep(i));
            return new SaveDefinition<T>(key, schema, defaults ?? (() => new T()), _ => SaveValidationResult.Valid(),
                steps, aliases ?? Array.Empty<string>(), new PlainJsonSaveCodec(), new Sha256SaveProtector(), policy);
        }
    }
    public sealed class MemoryStore : ISaveStore
    {
        public readonly Dictionary<string, byte[]> Primary = new Dictionary<string, byte[]>();
        public readonly Dictionary<string, byte[]> Backup = new Dictionary<string, byte[]>();
        public bool FailWrites;
        public int Writes, Quarantines, Reads;
        public SaveStoreReadResult ReadPrimary(string key) { Reads++; return Primary.TryGetValue(key, out var b) ? SaveStoreReadResult.Found(b) : SaveStoreReadResult.Missing(); }
        public SaveStoreReadResult ReadBackup(string key) { Reads++; return Backup.TryGetValue(key, out var b) ? SaveStoreReadResult.Found(b) : SaveStoreReadResult.Missing(); }
        public SaveStoreWriteResult Write(string key, byte[] bytes)
        {
            Writes++;
            if (FailWrites) return SaveStoreWriteResult.Failed("SAVE_WRITE_FAILED_PREVIOUS_RETAINED");
            if (Primary.TryGetValue(key, out var previous)) Backup[key] = (byte[])previous.Clone();
            Primary[key] = (byte[])bytes.Clone();
            return SaveStoreWriteResult.Succeeded(key);
        }
        public SaveStoreWriteResult RestoreBackup(string key)
        {
            if (!Backup.TryGetValue(key, out var bytes)) return SaveStoreWriteResult.Failed("SAVE_BACKUP_MISSING");
            Primary[key] = (byte[])bytes.Clone(); return SaveStoreWriteResult.Succeeded(key);
        }
        public SaveStoreWriteResult QuarantinePrimary(string key, byte[] bytes, string reason) { Quarantines++; Primary.Remove(key); return SaveStoreWriteResult.Succeeded(reason); }
        public SaveStoreWriteResult QuarantineBackup(string key, byte[] bytes, string reason) { Quarantines++; Backup.Remove(key); return SaveStoreWriteResult.Succeeded(reason); }
    }
    public sealed class Legacy : ILegacySaveSource
    {
        public int Reads;
        public string Json = "{\"Balance\":7,\"Receipts\":[\"claim.old\"]}";
        public bool TryRead(string key, out string json) { Reads++; json = Json; return true; }
    }
}

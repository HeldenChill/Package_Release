namespace Hung.Base.Persistence
{
    public interface ISaveStore
    {
        SaveStoreReadResult ReadPrimary(string key);
        SaveStoreReadResult ReadBackup(string key);
        SaveStoreWriteResult Write(string key, byte[] content);
        SaveStoreWriteResult RestoreBackup(string key);
        SaveStoreWriteResult QuarantinePrimary(string key, byte[] content, string reason);
        SaveStoreWriteResult QuarantineBackup(string key, byte[] content, string reason);
    }
}

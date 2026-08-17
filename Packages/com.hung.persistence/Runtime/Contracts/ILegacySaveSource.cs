namespace Hung.Data.Persistence
{
    /// <summary>
    /// Import-only, one-directional read path from a retired storage medium. Not a storage
    /// backend: implementations must never write or delete, and are consulted only when no
    /// canonical data exists yet.
    /// </summary>
    public interface ILegacySaveSource
    {
        bool TryRead(string key, out string json);
    }
}

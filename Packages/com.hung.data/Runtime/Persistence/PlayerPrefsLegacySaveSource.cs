using UnityEngine;

namespace Hung.Data.Persistence
{
    // ponytail: import-only shim. PlayerPrefs is retired as a storage medium
    // (owner decision 2026-08-17). This reader exists solely so plan 2 can import
    // PVM's existing PlayerPrefs data once. Delete at plan 2 cutover, after which
    // ILegacySaveSource has no PlayerPrefs implementation.
    public sealed class PlayerPrefsLegacySaveSource : ILegacySaveSource
    {
        public bool TryRead(string key, out string json)
        {
            if (PlayerPrefs.HasKey(key))
            {
                json = PlayerPrefs.GetString(key);
                return true;
            }

            json = null;
            return false;
        }
    }
}

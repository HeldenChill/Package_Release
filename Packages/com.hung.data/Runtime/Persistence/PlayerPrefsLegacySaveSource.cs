using UnityEngine;

namespace Hung.Data.Persistence
{
    public interface ILegacySaveSource
    {
        bool TryRead(string key, out string json);
    }

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

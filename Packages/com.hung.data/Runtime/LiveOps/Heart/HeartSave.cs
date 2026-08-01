using System;
using UnityEngine;

public static class HeartSave
{
    [Serializable]
    public class HeartSaveData
    {
        public int addMaxHearts = 0;
        public int defaultMaxHearts = 5;
        public string lastSaveTime = "";
        public string unlimitedHeartExpireTime = "";
    }
    #region Save Data

    private static HeartSaveData _saveData;
    private static HeartSaveData SaveData
    {
        get
        {
            if (_saveData == null)
            {
                _saveData = Database.Load<HeartSaveData>();
            }
            return _saveData;
        }
    }
    public static int MaxHearts => SaveData.addMaxHearts + SaveData.defaultMaxHearts;
    public static void SetDefautsMaxHearts(int newMax)
    {
        SaveData.defaultMaxHearts = newMax;
        Database.Save(_saveData);
    }
    public static void SetMaxHeart(int value)
    {
        SaveData.addMaxHearts = value - SaveData.defaultMaxHearts;
        Database.Save(_saveData);
    }
    public static string LastSaveTime => SaveData.lastSaveTime;
    public static void SetLastSaveTime(string time)
    {
        SaveData.lastSaveTime = time;
        Database.Save(_saveData);
    }
    public static string UnlimitedHeartExpireTime => SaveData.unlimitedHeartExpireTime;
    public static void SetUnlimitedExpireTime(string time)
    {
        SaveData.unlimitedHeartExpireTime = time;
        Database.Save(_saveData);
    }
    public static void Save()
    {
        Database.Save(_saveData);
    }
    #endregion
}

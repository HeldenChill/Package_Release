using System;
using System.Collections.Generic;
using UnityEngine;
using Hung.Base;

[CreateAssetMenu(fileName = "DailyGiftDataSO", menuName = "SubSystem/DailyGiftDataSO")]
public class DailyGiftDataSO : ScriptableObject
{
    public int levelUnlock = 3;
    public List<DailyGiftItem> listDailyGift;
    public List<DailyGiftItem> listStreakLoginGift;
}

[Serializable]
public class DailyGiftItem
{
    public int day;
    public List<GameData.ItemData> ListRwItem;
}

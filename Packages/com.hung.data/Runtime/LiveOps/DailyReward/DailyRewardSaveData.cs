using System;
using UnityEngine;


[Serializable]
public class DailyRewardSaveData
{
    public int currentProgress;
    public int dayOfYear = DateTime.Now.DayOfYear;

    // save the current time in sec
    public int lastFreeClaimTime;

    public bool IsVerifyNewDay()
    {
        if (dayOfYear == DateTime.Now.DayOfYear) return false;
        currentProgress = 0;
        dayOfYear = DateTime.Now.DayOfYear;
        return true;
    }
}



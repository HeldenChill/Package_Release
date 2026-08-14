using System;
using UnityEngine;


[Serializable]
public class DailyRewardSaveData
{
    public int currentProgress;
    public int dayOfYear;
    public int rewardDayKey;

    // save the current time in sec
    public int lastFreeClaimTime;
    public long lastFreeClaimUtcTicks;

    public bool IsVerifyNewDay()
    {
        return false;
    }
}



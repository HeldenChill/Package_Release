using System;
using Newtonsoft.Json;
using UnityEngine;


[Serializable]
public class DailyRewardSaveData
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Hung.Data.Rewards.PendingRewardData bw4Pending;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string bw4ConsumedClaimId;
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

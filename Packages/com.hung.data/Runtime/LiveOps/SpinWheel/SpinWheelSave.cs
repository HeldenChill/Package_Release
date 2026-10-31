using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class SpinWheelSaveData
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Hung.Data.Rewards.PendingRewardData bw4Pending;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string bw4ConsumedClaimId;
    public bool isDoneSpinFreeToday;
    public int adsSpinToday;
    public int dayOfYear;
    public int rewardDayKey;
    public int spinOrdinal;
    public string pendingSpinId;
    public int pendingSelectedIndex;
    public bool pendingIsAds;
    public string pendingPayloadFingerprint;

    public bool IsVerifyNewDay()
    {
        return false;
    }
}

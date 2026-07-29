using System;
using UnityEngine;

[Serializable]
public class SpinWheelSaveData
{
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


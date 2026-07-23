using System;
using UnityEngine;

[Serializable]
public class SpinWheelSaveData
{
    public bool isDoneSpinFreeToday;
    public int adsSpinToday;
    public int dayOfYear = DateTime.Now.DayOfYear;

    public bool IsVerifyNewDay()
    {
        if (dayOfYear == DateTime.Now.DayOfYear) return false;
        isDoneSpinFreeToday = false;
        adsSpinToday = 0;
        dayOfYear = DateTime.Now.DayOfYear;
        return true;
    }
}


using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine;


[CreateAssetMenu(fileName = "SpinDataSO", menuName = "HoangHH/SpinWheelDataSO")]
public class SpinWheelDataSO : ScriptableObject
{
    [SerializeField] private List<SpinItem> spinFreeItems;
    [SerializeField] private int adsSpinPerDay = 2;
    [SerializeField] private List<SpinItem> spinAdsItems;
    [SerializeField] private int resetOffsetMinutes;

    public List<SpinItem> SpinFreeItems => spinFreeItems;
    public int AdsSpinPerDay => adsSpinPerDay;
    public List<SpinItem> SpinAdsItems => spinAdsItems;
    public int ResetOffsetMinutes => resetOffsetMinutes;

    public SpinItem FreeItem(int index)
    {
        if (index >= 0 && index < spinFreeItems.Count) return spinFreeItems[index];
        return null;
    }

    public SpinItem AdsItem(int index)
    {
        if (index >= 0 && index < spinAdsItems.Count) return spinAdsItems[index];
        return null;
    }

    public int GetRandomFreeItemIndex()
    {
        return GetRandomIndex(spinFreeItems);
    }

    public int GetRandomAdsItemIndex()
    {
        return GetRandomIndex(spinAdsItems);
    }

    private static int GetRandomIndex(List<SpinItem> spinItems)
    {
        // get random index based on the rate, assume total rate of all equal to 1
        float randomRate = UnityEngine.Random.Range(0f, 1f);
        float totalRate = 0;
        for (int i = 0; i < spinItems.Count; i++)
        {
            totalRate += spinItems[i].rate;
            if (randomRate <= totalRate)
            {
                return i;
            }
        }
        return spinItems.Count - 1;
    }
}


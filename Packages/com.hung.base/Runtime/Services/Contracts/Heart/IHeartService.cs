using System;
using UnityEngine;

public interface IHeartService
{
    public Action<int> OnHeartsChanged { get; set; }
    public int CurrentHearts { get; }
    public int MaxHearts { get; }
    public float RemainingTime { get; }
    public string GetHeartFormattedTimeRemaining();
    public string GetUnlimitedHeartFormattedTimeRemaining();
    public bool UseHeart(bool isLock = false);
    public void UnlimitedHeart(int unlimitedTime);
    public void CheckLockHeart();
    public void AddHearts(int amount);
    public void RefillHearts();
    public void SetLockHeart(int value);
}

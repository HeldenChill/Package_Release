using Hung.Base;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "DailyRewardDataSO", menuName = "SubSystem/DailyRewardDataSO")]
public class DailyRewardDataSO : ScriptableObject
{
    [FormerlySerializedAs("freeReward")][SerializeField] private DailyRewardItem freeRewardItem;
    [SerializeField] private List<DailyRewardItem> rewardItems;

    public List<DailyRewardItem> RewardItems => rewardItems;
    public DailyRewardItem FreeRewardItem => freeRewardItem;
    public int TotalRewardItems => rewardItems.Count;

    public const int FREE_REWARD_COOLDOWN_SEC = 3600;
}

[Serializable]
public class DailyRewardItem
{
    [SerializeField] private ItemId itemId;
    public int value;
    public bool isAdsReward;

    public ItemId Type
    {
        get
        {
            return itemId;
        }
    }
}

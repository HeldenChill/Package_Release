using System.Collections.Generic;
using Hung.Base;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Framework gameplay config. Holds only what the framework itself reads: the item catalog and the
/// heart economy. A game adds its own tunables by declaring another <c>partial class GameplayData</c>
/// from its <c>Hung.Data.asmref</c> folder - same class, same asset, same field names, so existing
/// <c>GameplayData.asset</c> instances keep deserializing.
/// </summary>
[CreateAssetMenu(fileName = "GameplayData", menuName = "ScriptableObjects/Gameplay")]
public partial class GameplayData : SerializedScriptableObject
{
    [SerializeField]
    private int maxHearts;
    public int MaxHearts => maxHearts;
    [SerializeField]
    private float heartCooldownTime;
    public float HeartCooldownTime => heartCooldownTime;
    [SerializeField]
    private int reviveHeartCost;
    public int ReviveHeartCost => reviveHeartCost;
    [SerializeField]
    private Dictionary<ItemId, int> itemIdToMaxQuantity = new();
}

using Hung.Base;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "IAP Products", menuName = "ScriptableObjects/IAP Data")]
public class IAPData : SerializedScriptableObject
{
    [SerializeField]
    protected Dictionary<IAP_ITEM, IAPItem> purchaseProducts;

    public Dictionary<IAP_ITEM, IAPItem> PurchaseProducts => purchaseProducts;
}


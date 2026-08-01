using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    [Serializable]
    public class IAPItem
    {
        public string Name;
        public IAP_PRODUCT_TYPE Type;
        public string Id;
        public string Description;
        public string Price;
        public List<GameData.ItemData> rewards;
    }
}

using System;
using UnityEngine;

namespace Hung.Base
{
    [Serializable]
    public class SpinItem
    {
        [SerializeField] private ItemId itemId;
        public int value;
        public float rate;

        public ItemId Type
        {
            get
            {
                return itemId;
            }
        }

        // TEMP FOR THIS GAME, converting to RewardItemDbModel
    }
}

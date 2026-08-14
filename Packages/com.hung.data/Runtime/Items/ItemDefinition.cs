using System;
using Hung.Base;
using UnityEngine;

namespace Hung.Data
{
    [Serializable]
    public class ItemDefinition
    {
        [SerializeField] private ItemId id;
        [SerializeField] private string codeName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite showIcon;
        [SerializeField] private string displayName;
        [SerializeField] private ITEM_RARITY rarity;
        [SerializeField] private string description;
        [SerializeField] private int cost;
        [SerializeField] private int watchVideoCount;

        public ItemId Id => id;
        public string CodeName => codeName;
        public Sprite Icon => icon;
        public Sprite ShowIcon => showIcon;
        public string DisplayName => displayName;
        public ITEM_RARITY Rarity => rarity;
        public string Description => description;
        public int Cost => cost;
        public int WatchVideoCount => watchVideoCount;
    }
}

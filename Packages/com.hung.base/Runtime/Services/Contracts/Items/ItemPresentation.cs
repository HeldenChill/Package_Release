using UnityEngine;

namespace Hung.Base
{
    public readonly struct ItemPresentation
    {
        public ItemPresentation(Sprite icon, Sprite showIcon, string displayName, ITEM_RARITY rarity)
        {
            Icon = icon;
            ShowIcon = showIcon;
            DisplayName = displayName;
            Rarity = rarity;
        }

        public Sprite Icon { get; }
        public Sprite ShowIcon { get; }
        public string DisplayName { get; }
        public ITEM_RARITY Rarity { get; }
    }
}

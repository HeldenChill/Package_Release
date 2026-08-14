using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hung.Base
{
    public interface IItemService
    {
        public void ClaimItem(IReadOnlyList<GameData.ItemData> listRwItem, bool showDrop = false, Action callback = null)
        {
            ClaimItem(listRwItem.ToList(), showDrop, callback);
        }

        public void ClaimItem(ItemId item, int quantity, object data = null);
        public void SpendItem(ItemId item, int quantity);
        public void SetLock(ItemId item, int quantity);
        public GameData.ItemData GetItem(ItemId item);
        public void ShowReward(IReadOnlyList<ItemId> items, IReadOnlyList<int> quantitys);
        public void ShowBadge(ItemId item);

        public bool TryGetPresentation(ItemId item, out ItemPresentation presentation)
        {
            presentation = default;
            return false;
        }

        public ItemPresentation GetPresentation(ItemId item)
        {
            if (TryGetPresentation(item, out ItemPresentation presentation))
                return presentation;

            throw new KeyNotFoundException($"Item id '{item.Value}' presentation was not found.");
        }

        public void ClaimItem(List<GameData.ItemData> listRwItem, bool showDrop = false, Action callback = null);
        public void SaveItemData();
        public void ShowUI(bool value);
        public void SetSpawnPosition(Transform transform);
    }
}

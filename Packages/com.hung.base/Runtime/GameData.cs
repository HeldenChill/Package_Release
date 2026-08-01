using Hung.Base;
using Hung.Base.Persistence;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Database
{
    private static IPersistenceService _service;
    private static Func<IPersistenceService> _serviceFactory;

    public static Func<IPersistenceService> ServiceFactory
    {
        get => _serviceFactory;
        set
        {
            _serviceFactory = value;
            _service = null;
        }
    }

    public static ICompatibilitySaveDefinitionFactory CompatibilityDefinitionFactory { get; set; }

    public static IPersistenceService Service
    {
        get
        {
            if (_service != null)
                return _service;
            if (_serviceFactory == null)
                throw new InvalidOperationException(
                    "Database persistence is not configured. Install com.hung.data or assign Database.ServiceFactory.");
            return _service = _serviceFactory() ?? throw new InvalidOperationException("Database service factory returned null.");
        }
        set => _service = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static void Save<T>(T data, string key = null) where T : new()
    {
        SaveResult result = Service.Save(ResolveDefinition<T>(key), data);
        if (!result.Success)
            throw new PersistenceException(result.DiagnosticCode);
    }

    public static T Load<T>(string key = null) where T : new()
    {
        LoadResult<T> result = Service.Load(ResolveDefinition<T>(key));
        if (!result.Success)
            throw new PersistenceException(result.DiagnosticCode);
        return result.Value;
    }

    private static SaveDefinition<T> ResolveDefinition<T>(string key) where T : new()
    {
        IPersistenceService service = Service;
        if (service.TryGetDefinition(key, out SaveDefinition<T> registered))
            return registered;

        if (service.TryGetDefinition<T>(null, out _))
            throw new PersistenceException("SAVE_DEFINITION_KEY_MISMATCH");

        if (CompatibilityDefinitionFactory == null)
            throw new InvalidOperationException(
                "Database compatibility definitions are not configured. Install com.hung.data or assign Database.CompatibilityDefinitionFactory.");

        return CompatibilityDefinitionFactory.Create<T>(string.IsNullOrEmpty(key) ? typeof(T).Name : key);
    }
}

namespace Hung.Base
{
    [Serializable]
    public class ItemData
    {
        public ItemId Id;
        [PreviewField(75)]
        public Sprite Icon;
        [PreviewField(75)]
        public Sprite ShowIcon;
        public string Name;
        public ITEM_RARITY Rarity;
        public string Description;
        public int Cost;
        public int WatchVideoCount;
    }

    /// <summary>
    /// The framework save model. A game adds its own serialized members by declaring another
    /// <c>partial class GameData</c> from its <c>Hung.Base.asmref</c> folder.
    /// </summary>
    public partial class GameData
    {
        public const string SaveKey = "GameData.item-id-v1";

        public SettingData setting = new();
        public UserData user = new();
        public LevelData level = new();
        public bool IsFirstTimeUser = true;

        public bool InitData(IEnumerable<ItemId> itemIds)
        {
            IsFirstTimeUser = false;
            List<ItemId> items = itemIds
                .Where(id => id.IsValid)
                .Distinct()
                .ToList();

            if (user.PurchasedItems == null)
            {
                user.PurchasedItems = new List<IAP_ITEM>();
            }
            if (user.RewardGrantReceipts == null)
            {
                user.RewardGrantReceipts = new List<RewardGrantReceiptData>();
            }
            if (user.ItemDatas == null)
            {
                user.ItemDatas = Array.Empty<ItemData>();
                IsFirstTimeUser = true;
            }

            foreach (ItemData data in user.ItemDatas)
            {
                EnsureItemId(data);
            }

            var dataById = user.ItemDatas
                .Where(data => data != null && data.ItemId.IsValid)
                .GroupBy(data => data.ItemId)
                .ToDictionary(group => group.Key, group => group.First());

            var merged = new List<ItemData>(user.ItemDatas.Where(data => data != null));
            foreach (ItemId item in items)
            {
                if (dataById.ContainsKey(item))
                    continue;

                merged.Add(new ItemData
                {
                    ItemId = item,
                    Quantity = 0
                });
            }

            user.ItemDatas = merged.ToArray();
            return IsFirstTimeUser;
        }
        public int TotalStar
        {
            get
            {
                int value = 0;
                for (int i = 0; i < level.LevelStars.Count; i++)
                {
                    value += level.LevelStars[i];
                }
                return value;
            }
        }

        public void SetStar(int level, int star)
        {
            if (level >= this.level.LevelStars.Count)
            {
                int count = level - this.level.LevelStars.Count;
                for (int i = 0; i <= count; i++)
                {
                    this.level.LevelStars.Add(0);
                }
                count = level - this.level.PassLevels.Count;
                for (int i = 0; i <= count; i++)
                {
                    this.level.PassLevels.Add(false);
                }
            }
            this.level.DeltaStar = Mathf.Clamp(star - this.level.LevelStars[level], 0, 3);
            if(star > this.level.LevelStars[level])
            {
                this.level.LevelStars[level] = star;
            }
        }
        public int ClaimItem(ItemId item, int value)
        {
            Locator.Analytics?.EarnVirtualCurrency(item.Value, value, "");
            ItemData data = GetItemData(item);
            data.Quantity += value;
            return data.Quantity;
        }
        public int SpendItem(ItemId item, int value)
        {
            Locator.Analytics?.SpendVirtualCurrency(item.Value, value, "");
            ItemData data = GetItemData(item);
            data.Quantity -= value;
            return data.Quantity;
        }
        public int SetLock(ItemId item, int value)
        {
            ItemData data = GetItemData(item);
            data.LockQuantity = value;
            return data.LockQuantity;
        }
        public ItemData GetItemData(ItemId item)
        {
            ItemData data = user.ItemDatas.FirstOrDefault(x => x != null && EnsureItemId(x) == item);
            if (data == null)
                throw new KeyNotFoundException($"Item id '{item.Value}' was not found in save data.");

            return data;
        }

        public bool IsRemoveAds()
        {
            return GetItemData(BaseItemIds.RemoveAds).Quantity > 0 ||
                   GetItemData(BaseItemIds.PremiumRemoveAds).Quantity > 0;
        }
        public bool IsPremiumRemoveAds()
        {
            return GetItemData(BaseItemIds.PremiumRemoveAds).Quantity > 0;
        }

        private static ItemId EnsureItemId(ItemData data)
        {
            if (data == null)
                return default;

            if (data.ItemId.IsValid)
                return data.ItemId;

            return data.ItemId;
        }

        [Serializable]
        public class UserData
        {
            // Level Progress Data
            public int normalLevelIndex;
            public int currentLevelIndex;
            public int maxHearts;
            public int watchingAdsCount = 0;
            public int playGameAdsCount = 0;
            public string lastHeartSaveTime;
            // Item Data
            public ItemData[] ItemDatas;
            public List<IAP_ITEM> PurchasedItems;
            public List<RewardGrantReceiptData> RewardGrantReceipts = new();
        }

        [Serializable]
        public sealed class RewardGrantReceiptData
        {
            public string ClaimId;
            public string PayloadFingerprint;
            public long GrantedUtcTicks;
        }

        [Serializable]
        public class SettingData
        {
            public bool hapticOff;
            public bool isBgmMute;
            public bool isSfxMute;
        }
        [Serializable]
        public class ItemData
        {
            public ItemId ItemId;
            public int Quantity;
            public int LockQuantity;
        }
        [Serializable]
        public class LevelData
        {
            public int Action;
            // ponytail: Knife is TemplateGame-shaped but it is a serialized save field - moving it
            // out is a save-format break (ADR-E5). Left in place deliberately.
            public int Knife;
            public int Star;
            public int DeltaStar;
            public List<int> LevelStars = new List<int>();
            public List<bool> PassLevels = new List<bool>();
        }
    }
}

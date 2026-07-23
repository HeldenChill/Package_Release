using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using UnityEngine;

namespace Hung.Data
{
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Hung/Items/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> definitions = new();

        private readonly Dictionary<ItemId, ItemDefinition> byId = new();
        private readonly Dictionary<string, ItemDefinition> byCodeName = new(StringComparer.Ordinal);
        private List<ItemId> sortedIds = new();

        public IReadOnlyList<ItemId> Ids => sortedIds;

        private void OnEnable()
        {
            RebuildIndex();
        }

        public void RebuildIndex()
        {
            byId.Clear();
            byCodeName.Clear();

            for (int i = 0; i < definitions.Count; i++)
            {
                ItemDefinition definition = definitions[i];
                if (definition == null)
                    throw new InvalidOperationException($"Item catalog entry {i} is null.");

                ItemId id = definition.Id;
                if (!id.IsValid)
                    throw new InvalidOperationException($"Item catalog entry {i} has invalid id '{id}'.");

                if (byId.ContainsKey(id))
                    throw new InvalidOperationException($"Item catalog entry {i} duplicates id '{id.Value}'.");

                string codeName = definition.CodeName;
                if (!string.IsNullOrEmpty(codeName) && byCodeName.ContainsKey(codeName))
                    throw new InvalidOperationException(
                        $"Item catalog entry {i} duplicates code name '{codeName}'.");

                byId.Add(id, definition);
                if (!string.IsNullOrEmpty(codeName))
                    byCodeName.Add(codeName, definition);
            }

            sortedIds = byId.Keys.OrderBy(id => id.Value, StringComparer.Ordinal).ToList();
        }

        public bool TryGet(ItemId id, out ItemDefinition definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        public ItemDefinition GetRequired(ItemId id)
        {
            if (TryGet(id, out ItemDefinition definition))
                return definition;

            throw new KeyNotFoundException($"Item id '{id.Value}' was not found in catalog '{name}'.");
        }
    }
}

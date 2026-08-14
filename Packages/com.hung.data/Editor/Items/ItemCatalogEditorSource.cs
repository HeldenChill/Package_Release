using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Base.Editor;
using UnityEditor;

namespace Hung.Data.Editor
{
    [InitializeOnLoad]
    public sealed class ItemCatalogEditorSource : IItemIdEditorSource
    {
        private readonly Func<IEnumerable<ItemCatalog>> scanCatalogs;

        static ItemCatalogEditorSource()
        {
            ItemIdEditorRegistry.RegisterSource(new ItemCatalogEditorSource());
        }

        public ItemCatalogEditorSource()
            : this(ScanCatalogs)
        {
        }

        public ItemCatalogEditorSource(Func<IEnumerable<ItemCatalog>> scanCatalogs)
        {
            this.scanCatalogs = scanCatalogs ?? throw new ArgumentNullException(nameof(scanCatalogs));
        }

        public IEnumerable<ItemIdEditorOption> GetOptions()
        {
            foreach (ItemCatalog catalog in scanCatalogs() ?? Array.Empty<ItemCatalog>())
            {
                if (catalog == null) continue;
                catalog.RebuildIndex();
                foreach (ItemId id in catalog.Ids)
                {
                    string label = id.Value;
                    if (catalog.TryGet(id, out ItemDefinition definition) &&
                        !string.IsNullOrEmpty(definition.DisplayName))
                        label = definition.DisplayName;
                    yield return new ItemIdEditorOption(id, label, GetGroup(id));
                }
            }
        }

        private static IEnumerable<ItemCatalog> ScanCatalogs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ItemCatalog"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
                if (catalog != null) yield return catalog;
            }
        }

        private static string GetGroup(ItemId id)
        {
            int separator = id.Value.IndexOf('.', StringComparison.Ordinal);
            return separator > 0 ? id.Value.Substring(0, separator) : string.Empty;
        }
    }
}

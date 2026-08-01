using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using UnityEditor;
using UnityEngine;

namespace Hung.Data.Editor
{
    [InitializeOnLoad]
    public sealed class ItemIdDropdownDataSource
    {
        private static readonly ItemIdDropdownDataSource SharedSource = new ItemIdDropdownDataSource(ScanCatalogIds);

        private readonly Func<IEnumerable<ItemId>> scanIds;
        private IReadOnlyList<ItemIdDropdownOption> cachedOptions;

        static ItemIdDropdownDataSource()
        {
            EditorApplication.projectChanged -= InvalidateShared;
            EditorApplication.projectChanged += InvalidateShared;
            AssemblyReloadEvents.afterAssemblyReload -= InvalidateShared;
            AssemblyReloadEvents.afterAssemblyReload += InvalidateShared;
        }

        public ItemIdDropdownDataSource(Func<IEnumerable<ItemId>> scanIds)
        {
            this.scanIds = scanIds ?? throw new ArgumentNullException(nameof(scanIds));
        }

        public static IReadOnlyList<ItemIdDropdownOption> SharedOptions => SharedSource.GetOptions();

        public IReadOnlyList<ItemIdDropdownOption> GetOptions()
        {
            return cachedOptions ??= Build(scanIds());
        }

        public void Invalidate()
        {
            cachedOptions = null;
        }

        public static IReadOnlyList<ItemIdDropdownOption> Build(IEnumerable<ItemId> ids)
        {
            if (ids == null)
                return Array.Empty<ItemIdDropdownOption>();

            return ids
                .Where(id => id.IsValid)
                .Distinct()
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .Select(id => new ItemIdDropdownOption(id, GetGroupPath(id.Value)))
                .ToArray();
        }

        public static int FindSelectedIndex(IReadOnlyList<ItemIdDropdownOption> options, ItemId selected)
        {
            if (options == null)
                return -1;

            for (int index = 0; index < options.Count; index++)
            {
                if (options[index].Id == selected)
                    return index;
            }

            return -1;
        }

        private static string GetGroupPath(string value)
        {
            string[] segments = value.Split('.');
            return segments.Length > 1
                ? string.Join("/", segments, 0, segments.Length - 1)
                : string.Empty;
        }

        private static void InvalidateShared()
        {
            SharedSource.Invalidate();
        }

        private static IEnumerable<ItemId> ScanCatalogIds()
        {
            var ids = new List<ItemId>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemCatalog"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
                if (catalog == null)
                    continue;

                try
                {
                    catalog.RebuildIndex();
                    ids.AddRange(catalog.Ids);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to read item catalog '{path}': {exception.Message}", catalog);
                }
            }

            return ids;
        }
    }
}

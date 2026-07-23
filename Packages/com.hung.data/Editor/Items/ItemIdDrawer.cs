using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Hung.Data.Editor
{
    public sealed class ItemIdDrawer : OdinValueDrawer<ItemId>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ItemId current = ValueEntry.SmartValue;
            List<ItemId> ids = FindCatalogIds();

            if (ids.Count == 0)
            {
                string raw = EditorGUILayout.TextField(label, current.Value ?? string.Empty);
                if (raw != (current.Value ?? string.Empty) && ItemId.TryParse(raw, out ItemId typed))
                    ValueEntry.SmartValue = typed;
                return;
            }

            string display = current.IsValid ? current.Value : "<none>";
            if (!ids.Contains(current) && current.IsValid)
                SirenixEditorGUI.ErrorMessageBox($"Missing ItemId '{current.Value}' in item catalogs.");

            EditorGUILayout.BeginHorizontal();
            if (label != null && !string.IsNullOrEmpty(label.text) && label.text != "Item")
                EditorGUILayout.PrefixLabel(label);

            if (SirenixEditorGUI.ToolbarButton(new GUIContent(display, label?.text ?? "ItemId")))
            {
                var menu = new GenericMenu();
                foreach (ItemId id in ids)
                {
                    ItemId selected = id;
                    menu.AddItem(
                        new GUIContent(selected.Value),
                        selected == current,
                        () => ValueEntry.SmartValue = selected);
                }

                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static List<ItemId> FindCatalogIds()
        {
            var ids = new HashSet<ItemId>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemCatalog"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
                if (catalog == null)
                    continue;

                try
                {
                    catalog.RebuildIndex();
                    foreach (ItemId id in catalog.Ids)
                        ids.Add(id);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to read item catalog '{path}': {exception.Message}", catalog);
                }
            }

            return ids.OrderBy(id => id.Value, StringComparer.Ordinal).ToList();
        }
    }
}

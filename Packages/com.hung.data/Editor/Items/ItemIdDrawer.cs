using System.Collections.Generic;
using Hung.Base;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Hung.Data.Editor
{
    public sealed class ItemIdDrawer : OdinValueDrawer<ItemId>
    {
        private readonly AdvancedDropdownState dropdownState = new AdvancedDropdownState();

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ItemId current = ValueEntry.SmartValue;
            IReadOnlyList<ItemIdDropdownOption> options = ItemIdDropdownDataSource.SharedOptions;

            if (options.Count == 0)
            {
                DrawTextFallback(label, current);
                return;
            }

            bool missing = current.IsValid &&
                ItemIdDropdownDataSource.FindSelectedIndex(options, current) < 0;
            if (missing)
                SirenixEditorGUI.ErrorMessageBox($"Missing ItemId '{current.Value}' in item catalogs.");

            Rect row = EditorGUILayout.GetControlRect();
            Rect field = label == null || string.IsNullOrEmpty(label.text)
                ? row
                : EditorGUI.PrefixLabel(row, label);
            string display = current.IsValid ? current.Value : "<none>";

            if (!EditorGUI.DropdownButton(field, new GUIContent(display), FocusType.Keyboard))
                return;

            var dropdown = new ItemIdAdvancedDropdown(
                dropdownState,
                options,
                current,
                selected => ValueEntry.SmartValue = selected);
            dropdown.Show(field);
        }

        private void DrawTextFallback(GUIContent label, ItemId current)
        {
            string previous = current.Value ?? string.Empty;
            string raw = EditorGUILayout.TextField(label, previous);
            if (raw != previous && ItemId.TryParse(raw, out ItemId typed))
                ValueEntry.SmartValue = typed;
        }
    }
}

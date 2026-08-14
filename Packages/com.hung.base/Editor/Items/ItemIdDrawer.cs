using System.Collections.Generic;
using Hung.Base;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Hung.Base.Editor
{
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class ItemIdDrawer : OdinValueDrawer<ItemId>
    {
        private readonly AdvancedDropdownState dropdownState = new AdvancedDropdownState();

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ItemId current = ValueEntry.SmartValue;
            IReadOnlyList<ItemIdEditorOption> options = ItemIdEditorRegistry.Shared.GetOptions();
            if (current.IsValid && ItemIdEditorRegistry.Shared.IsUnknown(current))
                SirenixEditorGUI.ErrorMessageBox($"Missing ItemId '{current.Value}' in registered definitions.");

            Rect row = EditorGUILayout.GetControlRect();
            Rect field = label == null || string.IsNullOrEmpty(label.text) ? row : EditorGUI.PrefixLabel(row, label);
            string display = current.IsValid ? current.Value : "<none>";
            if (EditorGUI.DropdownButton(field, new GUIContent(display), FocusType.Keyboard))
            {
                var dropdown = new ItemIdAdvancedDropdown(dropdownState, options, current,
                    selected => ValueEntry.SmartValue = selected);
                dropdown.Show(field);
            }
        }
    }
}

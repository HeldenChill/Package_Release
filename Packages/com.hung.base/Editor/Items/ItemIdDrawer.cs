using Hung.Base;
using Sirenix.OdinInspector.Editor;
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
            StableIdField.Draw(
                label,
                ValueEntry.SmartValue,
                ItemIdEditorRegistry.Shared.Core,
                dropdownState,
                selected => ValueEntry.SmartValue = selected);
        }
    }
}

using Hung.Base;
using Sirenix.OdinInspector.Editor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Hung.Base.Editor
{
    /// <summary>Odin value drawer for <see cref="AudioId"/> properties.</summary>
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class AudioIdDrawer : OdinValueDrawer<AudioId>
    {
        private readonly AdvancedDropdownState dropdownState = new AdvancedDropdownState();

        /// <inheritdoc />
        protected override void DrawPropertyLayout(GUIContent label)
        {
            StableIdField.Draw(
                label,
                ValueEntry.SmartValue,
                StableIdEditorRegistry<AudioId>.Shared,
                dropdownState,
                selected => ValueEntry.SmartValue = selected);
        }
    }
}

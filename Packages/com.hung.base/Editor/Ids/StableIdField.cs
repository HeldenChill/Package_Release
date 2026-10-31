using System;
using System.Collections.Generic;
using Hung.Base;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Hung.Base.Editor
{
    /// <summary>Reusable IMGUI layout drawer for typed stable identifiers with search and dropdown support.</summary>
    public static class StableIdField
    {
        /// <summary>Draws an Odin/IMGUI property field for a typed stable identifier.</summary>
        /// <typeparam name="TId">The identifier struct type.</typeparam>
        /// <param name="label">GUI content label for the property.</param>
        /// <param name="current">The current identifier value.</param>
        /// <param name="registry">The editor registry supplying options and validation.</param>
        /// <param name="state">Persistent state for the dropdown popup.</param>
        /// <param name="onSelected">Callback invoked when a new identifier is selected from the dropdown.</param>
        public static void Draw<TId>(
            GUIContent label,
            TId current,
            StableIdEditorRegistry<TId> registry,
            AdvancedDropdownState state,
            Action<TId> onSelected) where TId : struct, IStableId, IEquatable<TId>
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            IReadOnlyList<StableIdEditorOption<TId>> options = registry.GetOptions();
            if (current.IsValid && registry.IsUnknown(current))
                SirenixEditorGUI.ErrorMessageBox($"Missing {typeof(TId).Name} '{current.Value}' in registered definitions.");

            Rect row = EditorGUILayout.GetControlRect();
            Rect field = label == null || string.IsNullOrEmpty(label.text) ? row : EditorGUI.PrefixLabel(row, label);
            string display = current.IsValid ? current.Value : "<none>";
            if (EditorGUI.DropdownButton(field, new GUIContent(display), FocusType.Keyboard))
            {
                var dropdown = new StableIdAdvancedDropdown<TId>(state, options, current, onSelected);
                dropdown.Show(field);
            }
        }
    }
}

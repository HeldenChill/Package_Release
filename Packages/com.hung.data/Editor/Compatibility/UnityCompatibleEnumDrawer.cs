using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Hung.Data.Editor
{
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class UnityCompatibleEnumDrawer<T> : OdinValueDrawer<T>
        where T : struct, Enum
    {
        public static bool IsFlagsEnum => typeof(T).IsDefined(typeof(FlagsAttribute), false);

        public static Enum Box(T value)
        {
            return (Enum)(object)value;
        }

        public static T Unbox(Enum value)
        {
            return (T)(object)value;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            T current = ValueEntry.SmartValue;
            EditorGUI.BeginChangeCheck();
            Enum selected = IsFlagsEnum
                ? EditorGUILayout.EnumFlagsField(label, Box(current))
                : EditorGUILayout.EnumPopup(label, Box(current));

            if (EditorGUI.EndChangeCheck())
                ValueEntry.SmartValue = Unbox(selected);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hung.AutoTest.Editor
{
    [CustomPropertyDrawer(typeof(AutoTestAssertionConfig))]
    public sealed class AutoTestAssertionConfigDrawer : PropertyDrawer
    {
        const float LineHeight = 18f;
        const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Mode selector + identity row + enabled/severity/timeout/threshold + stringParam/stringParam2/intParam/tolerance
            return (LineHeight + Spacing) * 10f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeProp = property.FindPropertyRelative("type");
            SerializedProperty assertionIdProp = property.FindPropertyRelative("assertionId");
            SerializedProperty enabledProp = property.FindPropertyRelative("enabled");
            SerializedProperty severityProp = property.FindPropertyRelative("severity");
            SerializedProperty timeoutProp = property.FindPropertyRelative("timeoutSeconds");
            SerializedProperty thresholdProp = property.FindPropertyRelative("threshold");
            SerializedProperty stringParamProp = property.FindPropertyRelative("stringParam");
            SerializedProperty stringParam2Prop = property.FindPropertyRelative("stringParam2");
            SerializedProperty intParamProp = property.FindPropertyRelative("intParam");
            SerializedProperty toleranceProp = property.FindPropertyRelative("tolerance");

            bool isStringMode = !string.IsNullOrEmpty(assertionIdProp.stringValue);

            Rect row = new Rect(position.x, position.y, position.width, LineHeight);

            Rect modeRect = new Rect(row.x, row.y, row.width * 0.3f, row.height);
            Rect identityRect = new Rect(row.x + row.width * 0.32f, row.y, row.width * 0.68f, row.height);

            bool newIsStringMode = EditorGUI.Popup(modeRect, isStringMode ? 1 : 0, new[] { "Enum", "String ID" }) == 1;

            if (newIsStringMode)
            {
                List<AutoTestAssertionDescriptor> descriptors = AutoTestAssertionRegistry.Descriptors.ToList();
                if (descriptors.Count > 0)
                {
                    string[] ids = descriptors.Select(d => d.Id).ToArray();
                    int currentIndex = System.Array.IndexOf(ids, assertionIdProp.stringValue);
                    int selected = EditorGUI.Popup(identityRect, currentIndex < 0 ? ids.Length : currentIndex,
                        ids.Concat(new[] { "(raw / unresolved)" }).ToArray());
                    if (selected >= 0 && selected < ids.Length)
                        assertionIdProp.stringValue = ids[selected];
                }

                Rect rawRect = new Rect(row.x, row.y + LineHeight + Spacing, row.width, LineHeight);
                assertionIdProp.stringValue = EditorGUI.TextField(rawRect, "Assertion ID", assertionIdProp.stringValue);
            }
            else
            {
                EditorGUI.PropertyField(identityRect, typeProp, GUIContent.none);
            }

            float y = row.y + (LineHeight + Spacing) * 2f;

            DrawField(ref y, position, enabledProp);
            DrawField(ref y, position, severityProp);
            DrawField(ref y, position, timeoutProp);
            DrawField(ref y, position, thresholdProp);
            DrawField(ref y, position, stringParamProp);
            DrawField(ref y, position, stringParam2Prop);
            DrawField(ref y, position, intParamProp);
            DrawField(ref y, position, toleranceProp);

            EditorGUI.EndProperty();
        }

        static void DrawField(ref float y, Rect position, SerializedProperty prop)
        {
            Rect rect = new Rect(position.x, y, position.width, LineHeight);
            EditorGUI.PropertyField(rect, prop);
            y += LineHeight + Spacing;
        }
    }
}

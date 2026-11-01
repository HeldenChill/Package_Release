using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Dropdown of the graph's blackboard keys, filtered by the BlackboardRef type. Accepts keys dragged from the blackboard panel.</summary>
    [CustomPropertyDrawer(typeof(BlackboardKeyRef), true)]
    public sealed class BlackboardKeyRefDrawer : PropertyDrawer
    {
        public const string DragDataKey = "Hung.AI.BlackboardKeyId";
        const string NoneLabel = "(none)";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty idProperty = property.FindPropertyRelative("keyId");
            List<BlackboardKey> options = Options(AIEditorUtil.GraphOf(property.serializedObject.targetObject), RefType());
            List<string> labels = Labels(options, idProperty.stringValue, out int index);
            var dropdown = new DropdownField(property.displayName, labels, index);
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            dropdown.AddToClassList("ai-key-ref");
            var glyph = new Label(AIGlyphs.KeyRef);
            glyph.AddToClassList("ai-key-ref-glyph");
            glyph.AddToClassList("ai-f-mono");
            dropdown.Q(className: "unity-base-popup-field__input")?.Insert(0, glyph);
            dropdown.RegisterValueChangedCallback(_ => Write(idProperty, IdAt(options, dropdown.index, idProperty.stringValue)));
            dropdown.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                if (DroppedKey(options) != null) DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            });
            dropdown.RegisterCallback<DragPerformEvent>(_ =>
            {
                BlackboardKey key = DroppedKey(options);
                if (key == null) return;
                DragAndDrop.AcceptDrag();
                Write(idProperty, key.Id);
                dropdown.SetValueWithoutNotify(labels[options.IndexOf(key) + 1]);
            });
            return dropdown;
        }

        /// <summary>IMGUI path, used when an IMGUI inspector draws the field.</summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty idProperty = property.FindPropertyRelative("keyId");
            List<BlackboardKey> options = Options(AIEditorUtil.GraphOf(property.serializedObject.targetObject), RefType());
            List<string> labels = Labels(options, idProperty.stringValue, out int index);
            EditorGUI.BeginProperty(position, label, property);
            int picked = EditorGUI.Popup(position, label.text, index, labels.ToArray());
            if (picked != index) idProperty.stringValue = IdAt(options, picked, idProperty.stringValue);
            EditorGUI.EndProperty();
        }

        Type RefType()
        {
            Type type = fieldInfo != null ? fieldInfo.FieldType : typeof(BlackboardKeyRef);
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
            return type;
        }

        static List<BlackboardKey> Options(AIGraphAsset graph, Type refType)
        {
            var options = new List<BlackboardKey>();
            if (graph == null) return options;
            foreach (BlackboardKey key in graph.Blackboard.Keys)
                if (BlackboardTypeMap.Accepts(refType, key.Type)) options.Add(key);
            return options;
        }

        static List<string> Labels(List<BlackboardKey> options, string currentId, out int index)
        {
            var labels = new List<string> { NoneLabel };
            index = 0;
            for (int i = 0; i < options.Count; i++)
            {
                labels.Add($"{options[i].Name}  ({options[i].Type})");
                if (options[i].Id == currentId) index = i + 1;
            }
            if (index == 0 && !string.IsNullOrEmpty(currentId))
            {
                labels.Add("(missing key) " + currentId);
                index = labels.Count - 1;
            }
            return labels;
        }

        static string IdAt(List<BlackboardKey> options, int index, string currentId)
        {
            if (index <= 0) return string.Empty;
            return index - 1 < options.Count ? options[index - 1].Id : currentId;
        }

        static BlackboardKey DroppedKey(List<BlackboardKey> options)
        {
            if (!(DragAndDrop.GetGenericData(DragDataKey) is string keyId)) return null;
            return options.Find(k => k.Id == keyId);
        }

        static void Write(SerializedProperty idProperty, string id)
        {
            idProperty.serializedObject.Update();
            idProperty.stringValue = id;
            idProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}

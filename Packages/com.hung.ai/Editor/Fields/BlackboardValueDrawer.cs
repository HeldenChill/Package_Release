using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Shows only the BlackboardValue field that matches the type of the key chosen next to it (sibling "key", "KeyId" or "Id").</summary>
    [CustomPropertyDrawer(typeof(BlackboardValue))]
    public sealed class BlackboardValueDrawer : PropertyDrawer
    {
        static readonly string[] AllFields = { "BoolValue", "IntValue", "FloatValue", "StringValue", "Vector3Value", "ObjectValue" };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            Build(root, property);
            SerializedProperty keyId = Sibling(property, "key.keyId");
            if (keyId != null) root.TrackPropertyValue(keyId, _ => Build(root, property));
            return root;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BlackboardKey key = ResolveKey(property);
            if (key == null)
            {
                EditorGUI.LabelField(position, label.text, "Pick a blackboard key first.");
                return;
            }
            EditorGUI.PropertyField(position, property.FindPropertyRelative(FieldFor(key.Type)), label);
        }

        public static string FieldFor(BlackboardValueType type)
        {
            switch (type)
            {
                case BlackboardValueType.Bool: return "BoolValue";
                case BlackboardValueType.Int:
                case BlackboardValueType.Enum: return "IntValue";
                case BlackboardValueType.Float: return "FloatValue";
                case BlackboardValueType.String: return "StringValue";
                case BlackboardValueType.Vector3: return "Vector3Value";
                default: return "ObjectValue";
            }
        }

        static void Build(VisualElement root, SerializedProperty property)
        {
            root.Clear();
            BlackboardKey key = ResolveKey(property);
            if (key == null)
            {
                var foldout = new Foldout { text = property.displayName + " (pick a key to narrow this)", value = false };
                foreach (string name in AllFields)
                {
                    SerializedProperty child = property.FindPropertyRelative(name);
                    var field = new PropertyField(child);
                    field.BindProperty(child);
                    foldout.Add(field);
                }
                root.Add(foldout);
                return;
            }
            SerializedProperty valueProperty = property.FindPropertyRelative(FieldFor(key.Type));
            var valueField = new PropertyField(valueProperty, $"{property.displayName} ({key.Type})");
            valueField.BindProperty(valueProperty);
            root.Add(valueField);
        }

        static BlackboardKey ResolveKey(SerializedProperty property)
        {
            AIGraphAsset graph = AIEditorUtil.GraphOf(property.serializedObject.targetObject);
            if (graph == null) return null;
            SerializedProperty id = Sibling(property, "key.keyId") ?? Sibling(property, "KeyId") ?? Sibling(property, "Id");
            return id != null && id.propertyType == SerializedPropertyType.String ? graph.Blackboard.FindById(id.stringValue) : null;
        }

        static SerializedProperty Sibling(SerializedProperty property, string relativePath)
        {
            string path = property.propertyPath;
            int dot = path.LastIndexOf('.');
            string parent = dot < 0 ? string.Empty : path.Substring(0, dot);
            return property.serializedObject.FindProperty(parent.Length == 0 ? relativePath : parent + "." + relativePath);
        }
    }
}

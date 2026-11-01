using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>UI fields for one typed blackboard value, and live value text for the debugger.</summary>
    public static class BlackboardValueFields
    {
        public static VisualElement Create(BlackboardKey key, BlackboardValue value, Action<BlackboardValue> changed, string label, bool allowSceneObjects)
        {
            switch (key.Type)
            {
                case BlackboardValueType.Bool:
                {
                    var field = new Toggle(label) { value = value.BoolValue };
                    field.RegisterValueChangedCallback(e => { value.BoolValue = e.newValue; changed(value); });
                    return field;
                }
                case BlackboardValueType.Enum:
                {
                    Type enumType = ResolveEnum(key.EnumTypeName);
                    if (enumType == null) goto case BlackboardValueType.Int;
                    var field = new EnumField(label, (Enum)Enum.ToObject(enumType, value.IntValue));
                    field.RegisterValueChangedCallback(e => { value.IntValue = Convert.ToInt32(e.newValue); changed(value); });
                    return field;
                }
                case BlackboardValueType.Int:
                {
                    var field = new IntegerField(label) { value = value.IntValue, isDelayed = true };
                    field.RegisterValueChangedCallback(e => { value.IntValue = e.newValue; changed(value); });
                    return field;
                }
                case BlackboardValueType.Float:
                {
                    var field = new FloatField(label) { value = value.FloatValue, isDelayed = true };
                    field.RegisterValueChangedCallback(e => { value.FloatValue = e.newValue; changed(value); });
                    return field;
                }
                case BlackboardValueType.String:
                {
                    var field = new TextField(label) { value = value.StringValue ?? string.Empty, isDelayed = true };
                    field.RegisterValueChangedCallback(e => { value.StringValue = e.newValue; changed(value); });
                    return field;
                }
                case BlackboardValueType.Vector3:
                {
                    var field = new Vector3Field(label) { value = value.Vector3Value };
                    field.RegisterValueChangedCallback(e => { value.Vector3Value = e.newValue; changed(value); });
                    return field;
                }
                case BlackboardValueType.GameObject:
                {
                    var field = new ObjectField(label) { objectType = typeof(GameObject), allowSceneObjects = allowSceneObjects, value = value.ObjectValue as GameObject };
                    field.RegisterValueChangedCallback(e => { value.ObjectValue = e.newValue; changed(value); });
                    return field;
                }
                case BlackboardValueType.Transform:
                {
                    var field = new ObjectField(label) { objectType = typeof(Transform), allowSceneObjects = allowSceneObjects, value = value.ObjectValue as Transform };
                    field.RegisterValueChangedCallback(e => { value.ObjectValue = e.newValue; changed(value); });
                    return field;
                }
                default:
                    return new Label($"{label}: unsupported type {key.Type}");
            }
        }

        /// <summary>Finds an enum by assembly-qualified name, full name or short name. Null when not found.</summary>
        public static Type ResolveEnum(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (Type candidate in TypeCache.GetTypesDerivedFrom<Enum>())
                {
                    if (candidate.FullName != typeName && candidate.Name != typeName) continue;
                    type = candidate;
                    break;
                }
            }
            return type != null && type.IsEnum ? type : null;
        }

        public static string FormatLive(Blackboard blackboard, int index)
        {
            switch (blackboard.GetValueType(index))
            {
                case BlackboardValueType.Bool: return blackboard.Get<bool>(index) ? "true" : "false";
                case BlackboardValueType.Int:
                case BlackboardValueType.Enum: return blackboard.Get<int>(index).ToString();
                case BlackboardValueType.Float: return blackboard.Get<float>(index).ToString("0.###");
                case BlackboardValueType.String: return blackboard.Get<string>(index) ?? "(null)";
                case BlackboardValueType.Vector3: return blackboard.Get<Vector3>(index).ToString("F2");
                case BlackboardValueType.GameObject:
                {
                    GameObject go = blackboard.Get<GameObject>(index);
                    return go != null ? go.name : "(none)";
                }
                case BlackboardValueType.Transform:
                {
                    Transform t = blackboard.Get<Transform>(index);
                    return t != null ? t.name : "(none)";
                }
                default: return "?";
            }
        }

        /// <summary>Slot index of a key id in a runtime blackboard, without the missing-key warning. -1 when absent.</summary>
        public static int FindRuntimeIndex(Blackboard blackboard, string keyId)
        {
            for (int i = 0; i < blackboard.Count; i++)
                if (blackboard.GetKeyId(i) == keyId) return i;
            return -1;
        }
    }
}

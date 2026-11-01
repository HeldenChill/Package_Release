using System;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Which blackboard key types a key-reference field may point at.</summary>
    public static class BlackboardTypeMap
    {
        /// <summary>T of a BlackboardRef&lt;T&gt; field type, or null for an untyped BlackboardKeyRef.</summary>
        public static Type ValueTypeOf(Type refFieldType)
        {
            for (Type t = refFieldType; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BlackboardRef<>)) return t.GetGenericArguments()[0];
            return null;
        }

        public static bool Accepts(Type refFieldType, BlackboardValueType keyType)
        {
            Type valueType = ValueTypeOf(refFieldType);
            if (valueType == null) return true;
            if (valueType == typeof(bool)) return keyType == BlackboardValueType.Bool;
            if (valueType == typeof(int)) return keyType == BlackboardValueType.Int || keyType == BlackboardValueType.Enum;
            if (valueType == typeof(float)) return keyType == BlackboardValueType.Float;
            if (valueType == typeof(string)) return keyType == BlackboardValueType.String;
            if (valueType == typeof(Vector3)) return keyType == BlackboardValueType.Vector3;
            if (valueType == typeof(GameObject)) return keyType == BlackboardValueType.GameObject;
            if (valueType == typeof(Transform)) return keyType == BlackboardValueType.Transform;
            return false;
        }
    }
}

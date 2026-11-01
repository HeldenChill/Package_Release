using System;
using UnityEngine;
namespace Hung.AI
{
    public enum BlackboardValueType
    {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3,
        Vector3 = 4,
        GameObject = 5,
        Transform = 6,
        Enum = 7
    }

    /// <summary>Only Agent is implemented. Shared and Global are reserved; they warn and behave as Agent.</summary>
    public enum BlackboardScope
    {
        Agent = 0,
        Shared = 1,
        Global = 2
    }

    /// <summary>Serialized value holder. Only the field matching the key's type is used.</summary>
    [Serializable]
    public struct BlackboardValue
    {
        public bool BoolValue;
        public int IntValue;
        public float FloatValue;
        public string StringValue;
        public Vector3 Vector3Value;
        public UnityEngine.Object ObjectValue;
    }

    [Serializable]
    public sealed class BlackboardKey
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name;
        public BlackboardValueType Type;
        public BlackboardScope Scope;
        [Tooltip("Assembly-qualified enum type name. Only used when Type is Enum; the value is stored as int.")]
        public string EnumTypeName;
        public BlackboardValue DefaultValue;
    }

    /// <summary>Per-agent replacement for a key's default value.</summary>
    [Serializable]
    public sealed class BlackboardKeyOverride
    {
        public string KeyId;
        public BlackboardValue Value;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    internal abstract class BlackboardSlot
    {
        public readonly BlackboardKey Key;

        protected BlackboardSlot(BlackboardKey key)
        {
            Key = key;
        }
    }

    internal sealed class BlackboardSlot<T> : BlackboardSlot
    {
        public T Value;

        public BlackboardSlot(BlackboardKey key, T value) : base(key)
        {
            Value = value;
        }
    }

    /// <summary>Per-agent runtime values for the keys of a BlackboardDefinition. Typed slots; no boxing on reads or writes.</summary>
    public sealed class Blackboard
    {
        readonly BlackboardSlot[] slots;
        readonly Dictionary<string, int> indexById = new Dictionary<string, int>();
        readonly Dictionary<string, int> indexByName = new Dictionary<string, int>();
        readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>Raised with the key id when a value actually changes.</summary>
        public event Action<string> KeyChanged;

        public Blackboard(BlackboardDefinition definition, IReadOnlyList<BlackboardKeyOverride> overrides = null)
        {
            IReadOnlyList<BlackboardKey> keys = definition != null ? definition.Keys : Array.Empty<BlackboardKey>();
            slots = new BlackboardSlot[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                BlackboardKey key = keys[i];
                if (key.Scope != BlackboardScope.Agent)
                    Debug.LogWarning($"[Hung.AI] Blackboard key '{key.Name}' uses scope {key.Scope}, which is reserved. It behaves as Agent scope in this version.");
                BlackboardValue value = key.DefaultValue;
                if (overrides != null)
                    for (int o = 0; o < overrides.Count; o++)
                        if (overrides[o] != null && overrides[o].KeyId == key.Id) value = overrides[o].Value;
                slots[i] = CreateSlot(key, value);
                indexById[key.Id] = i;
                string name = key.Name ?? string.Empty;
                if (!indexByName.ContainsKey(name)) indexByName[name] = i;
            }
        }

        public int Count => slots.Length;

        public string GetKeyName(int index) => slots[index].Key.Name;

        public string GetKeyId(int index) => slots[index].Key.Id;

        public BlackboardValueType GetValueType(int index) => slots[index].Key.Type;

        public int IndexOfName(string name) => name != null && indexByName.TryGetValue(name, out int i) ? i : -1;

        public int IndexOf(BlackboardKeyRef keyRef) => Resolve(keyRef);

        public T Get<T>(BlackboardKeyRef keyRef) => Get<T>(Resolve(keyRef));

        public T Get<T>(int index)
        {
            if (index < 0 || index >= slots.Length) return default;
            if (slots[index] is BlackboardSlot<T> slot) return slot.Value;
            WarnTypeMismatch<T>(index);
            return default;
        }

        public bool Set<T>(BlackboardKeyRef keyRef, T value) => Set(Resolve(keyRef), value);

        public bool Set<T>(int index, T value)
        {
            if (index < 0 || index >= slots.Length) return false;
            if (slots[index] is BlackboardSlot<T> slot)
            {
                if (EqualityComparer<T>.Default.Equals(slot.Value, value)) return true;
                slot.Value = value;
                KeyChanged?.Invoke(slot.Key.Id);
                return true;
            }
            WarnTypeMismatch<T>(index);
            return false;
        }

        /// <summary>True when the key holds a value: bool true, number not zero, string not empty, Vector3 not zero, object not null.</summary>
        public bool IsSet(BlackboardKeyRef keyRef)
        {
            int i = Resolve(keyRef);
            if (i < 0) return false;
            switch (slots[i].Key.Type)
            {
                case BlackboardValueType.Bool: return ((BlackboardSlot<bool>)slots[i]).Value;
                case BlackboardValueType.Int:
                case BlackboardValueType.Enum: return ((BlackboardSlot<int>)slots[i]).Value != 0;
                case BlackboardValueType.Float: return ((BlackboardSlot<float>)slots[i]).Value != 0f;
                case BlackboardValueType.String: return !string.IsNullOrEmpty(((BlackboardSlot<string>)slots[i]).Value);
                case BlackboardValueType.Vector3: return ((BlackboardSlot<Vector3>)slots[i]).Value != Vector3.zero;
                case BlackboardValueType.GameObject: return ((BlackboardSlot<GameObject>)slots[i]).Value != null;
                case BlackboardValueType.Transform: return ((BlackboardSlot<Transform>)slots[i]).Value != null;
                default: return false;
            }
        }

        /// <summary>Reads a world position from a Vector3, Transform, or GameObject key.</summary>
        public bool TryGetPosition(BlackboardKeyRef keyRef, out Vector3 position)
        {
            position = default;
            int i = Resolve(keyRef);
            if (i < 0) return false;
            switch (slots[i].Key.Type)
            {
                case BlackboardValueType.Vector3:
                    position = ((BlackboardSlot<Vector3>)slots[i]).Value;
                    return true;
                case BlackboardValueType.Transform:
                    Transform t = ((BlackboardSlot<Transform>)slots[i]).Value;
                    if (t == null) return false;
                    position = t.position;
                    return true;
                case BlackboardValueType.GameObject:
                    GameObject g = ((BlackboardSlot<GameObject>)slots[i]).Value;
                    if (g == null) return false;
                    position = g.transform.position;
                    return true;
                default:
                    WarnOnce("position:" + slots[i].Key.Id, $"[Hung.AI] Blackboard key '{slots[i].Key.Name}' is {slots[i].Key.Type}; a position needs Vector3, Transform or GameObject.");
                    return false;
            }
        }

        /// <summary>Writes the field of value that matches the key's type.</summary>
        public bool SetFromValue(BlackboardKeyRef keyRef, in BlackboardValue value)
        {
            int i = Resolve(keyRef);
            if (i < 0) return false;
            switch (slots[i].Key.Type)
            {
                case BlackboardValueType.Bool: return Set(i, value.BoolValue);
                case BlackboardValueType.Int:
                case BlackboardValueType.Enum: return Set(i, value.IntValue);
                case BlackboardValueType.Float: return Set(i, value.FloatValue);
                case BlackboardValueType.String: return Set(i, value.StringValue);
                case BlackboardValueType.Vector3: return Set(i, value.Vector3Value);
                case BlackboardValueType.GameObject: return Set(i, value.ObjectValue as GameObject);
                case BlackboardValueType.Transform: return Set(i, value.ObjectValue as Transform);
                default: return false;
            }
        }

        internal int Resolve(BlackboardKeyRef keyRef)
        {
            if (keyRef == null || !keyRef.IsAssigned) return -1;
            if (keyRef.boundTo == this) return keyRef.slot;
            keyRef.boundTo = this;
            keyRef.slot = indexById.TryGetValue(keyRef.KeyId, out int i) ? i : -1;
            if (keyRef.slot < 0)
                WarnOnce("missing:" + keyRef.KeyId, $"[Hung.AI] Blackboard key id '{keyRef.KeyId}' does not exist on this graph. Reads return the default value.");
            return keyRef.slot;
        }

        static BlackboardSlot CreateSlot(BlackboardKey key, in BlackboardValue v)
        {
            switch (key.Type)
            {
                case BlackboardValueType.Bool: return new BlackboardSlot<bool>(key, v.BoolValue);
                case BlackboardValueType.Int:
                case BlackboardValueType.Enum: return new BlackboardSlot<int>(key, v.IntValue);
                case BlackboardValueType.Float: return new BlackboardSlot<float>(key, v.FloatValue);
                case BlackboardValueType.String: return new BlackboardSlot<string>(key, v.StringValue);
                case BlackboardValueType.Vector3: return new BlackboardSlot<Vector3>(key, v.Vector3Value);
                case BlackboardValueType.GameObject: return new BlackboardSlot<GameObject>(key, v.ObjectValue as GameObject);
                case BlackboardValueType.Transform: return new BlackboardSlot<Transform>(key, v.ObjectValue as Transform);
                default: return new BlackboardSlot<int>(key, 0);
            }
        }

        void WarnTypeMismatch<T>(int index)
        {
            BlackboardKey key = slots[index].Key;
            WarnOnce("type:" + key.Id + ":" + typeof(T).Name, $"[Hung.AI] Blackboard key '{key.Name}' is {key.Type}; it was read or written as {typeof(T).Name}.");
        }

        void WarnOnce(string token, string message)
        {
            if (warned.Add(token)) Debug.LogWarning(message);
        }
    }
}

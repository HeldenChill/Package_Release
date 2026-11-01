using System;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>Serialized reference to a blackboard key by id. Caches its slot per Blackboard.</summary>
    [Serializable]
    public class BlackboardKeyRef
    {
        [SerializeField] string keyId;
        [NonSerialized] internal Blackboard boundTo;
        [NonSerialized] internal int slot;

        public BlackboardKeyRef() { }

        public BlackboardKeyRef(string keyId)
        {
            this.keyId = keyId;
        }

        public string KeyId
        {
            get => keyId;
            set { keyId = value; boundTo = null; }
        }

        public bool IsAssigned => !string.IsNullOrEmpty(keyId);
    }

    /// <summary>Typed key reference; the type lets the editor filter the key dropdown.</summary>
    [Serializable]
    public class BlackboardRef<T> : BlackboardKeyRef
    {
        public BlackboardRef() { }

        public BlackboardRef(string keyId) : base(keyId) { }
    }
}

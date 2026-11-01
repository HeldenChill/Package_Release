using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>Keys declared on a graph asset. Each agent builds its own Blackboard from it.</summary>
    [Serializable]
    public sealed class BlackboardDefinition
    {
        [SerializeField] List<BlackboardKey> keys = new List<BlackboardKey>();

        public IReadOnlyList<BlackboardKey> Keys => keys ??= new List<BlackboardKey>();

        public BlackboardKey Add(string name, BlackboardValueType type)
        {
            var key = new BlackboardKey { Name = name, Type = type };
            (keys ??= new List<BlackboardKey>()).Add(key);
            return key;
        }

        public bool Remove(string keyId)
        {
            return keys != null && keys.RemoveAll(k => k.Id == keyId) > 0;
        }

        public BlackboardKey FindByName(string name)
        {
            if (keys == null) return null;
            for (int i = 0; i < keys.Count; i++) if (keys[i].Name == name) return keys[i];
            return null;
        }

        public BlackboardKey FindById(string id)
        {
            if (keys == null) return null;
            for (int i = 0; i < keys.Count; i++) if (keys[i].Id == id) return keys[i];
            return null;
        }
    }
}

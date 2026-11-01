using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Text format for copy, cut, paste and duplicate of nodes. Ids are remapped on paste.</summary>
    public static class AIGraphClipboard
    {
        public const string Prefix = "HungAIGraph:";

        [Serializable]
        sealed class ClipboardNode
        {
            public string Type;
            public string Json;
        }

        [Serializable]
        sealed class ClipboardKey
        {
            public string Id;
            public string Name;
            public BlackboardValueType Type;
        }

        [Serializable]
        sealed class ClipboardData
        {
            public List<ClipboardNode> Nodes = new List<ClipboardNode>();
            public List<ClipboardKey> Keys = new List<ClipboardKey>();
        }

        public static string Serialize(AIGraphAsset asset, IEnumerable<string> nodeIds)
        {
            var data = new ClipboardData();
            if (asset != null && nodeIds != null)
            {
                var keyIds = new HashSet<string>();
                foreach (string id in nodeIds)
                {
                    AINode node = asset.GetNode(id);
                    if (node == null) continue;
                    data.Nodes.Add(new ClipboardNode { Type = node.GetType().AssemblyQualifiedName, Json = JsonUtility.ToJson(node) });
                    foreach (BlackboardKeyRef keyRef in AIGraphValidator.KeyRefsOf(node))
                    {
                        BlackboardKey key = keyRef.IsAssigned ? asset.Blackboard.FindById(keyRef.KeyId) : null;
                        if (key != null && keyIds.Add(key.Id)) data.Keys.Add(new ClipboardKey { Id = key.Id, Name = key.Name, Type = key.Type });
                    }
                }
            }
            return Prefix + JsonUtility.ToJson(data);
        }

        public static bool CanPaste(string text) => !string.IsNullOrEmpty(text) && text.StartsWith(Prefix, StringComparison.Ordinal);

        public static List<AINode> Paste(AIGraphAsset asset, string text, Vector2 offset)
        {
            var pasted = new List<AINode>();
            if (asset == null || !CanPaste(text)) return pasted;
            ClipboardData data;
            try
            {
                data = JsonUtility.FromJson<ClipboardData>(text.Substring(Prefix.Length));
            }
            catch (ArgumentException)
            {
                return pasted;
            }
            if (data == null || data.Nodes == null) return pasted;
            var idMap = new Dictionary<string, string>();
            foreach (ClipboardNode entry in data.Nodes)
            {
                Type type = string.IsNullOrEmpty(entry.Type) ? null : Type.GetType(entry.Type);
                if (type == null || type.IsAbstract || !typeof(AINode).IsAssignableFrom(type)) continue;
                var node = (AINode)Activator.CreateInstance(type);
                JsonUtility.FromJsonOverwrite(entry.Json, node);
                string newId = Guid.NewGuid().ToString("N");
                idMap[node.Id] = newId;
                node.Id = newId;
                node.EditorPosition += offset;
                pasted.Add(node);
            }
            if (pasted.Count == 0) return pasted;
            Undo.RecordObject(asset, "Paste AI Nodes");
            var childIds = new HashSet<string>();
            foreach (AINode node in pasted)
            {
                RemapChildren(node, idMap);
                RebindKeys(node, asset, data.Keys);
                foreach (string childId in AIGraphAsset.ChildIdsOf(node)) childIds.Add(childId);
                asset.AddNode(node);
            }
            if (asset.Root == null)
            {
                foreach (AINode node in pasted)
                {
                    if (childIds.Contains(node.Id)) continue;
                    asset.RootNodeId = node.Id;
                    break;
                }
            }
            EditorUtility.SetDirty(asset);
            return pasted;
        }

        /// <summary>Keeps refs whose key exists here with a fitting type; otherwise binds to the key with the copied key's name, or clears.</summary>
        static void RebindKeys(AINode node, AIGraphAsset asset, List<ClipboardKey> copiedKeys)
        {
            foreach (var (fieldType, keyRef) in AIGraphValidator.KeyRefFieldsOf(node))
            {
                if (!keyRef.IsAssigned) continue;
                BlackboardKey same = asset.Blackboard.FindById(keyRef.KeyId);
                if (same != null && BlackboardTypeMap.Accepts(fieldType, same.Type)) continue;
                ClipboardKey copied = copiedKeys?.Find(k => k.Id == keyRef.KeyId);
                BlackboardKey byName = copied != null ? asset.Blackboard.FindByName(copied.Name) : null;
                keyRef.KeyId = byName != null && BlackboardTypeMap.Accepts(fieldType, byName.Type) ? byName.Id : null;
            }
        }

        static void RemapChildren(AINode node, Dictionary<string, string> idMap)
        {
            if (node is CompositeNode composite)
            {
                var old = new List<string>(composite.ChildIds);
                foreach (string id in old) composite.RemoveChild(id);
                foreach (string id in old)
                    if (idMap.TryGetValue(id, out string mapped)) composite.AddChild(mapped);
            }
            else if (node is DecoratorNode decorator)
            {
                decorator.ChildId = !string.IsNullOrEmpty(decorator.ChildId) && idMap.TryGetValue(decorator.ChildId, out string mapped) ? mapped : null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Every graph edit the editor makes. Each method records Undo on the asset and marks it dirty.</summary>
    public static class AIGraphEditing
    {
        public static AINode CreateNode(AIGraphAsset asset, Type type, Vector2 position)
        {
            if (asset == null || type == null || type.IsAbstract || !typeof(AINode).IsAssignableFrom(type)) return null;
            Record(asset, "Create AI Node");
            var node = (AINode)Activator.CreateInstance(type);
            node.EditorPosition = position;
            asset.AddNode(node);
            if (asset.Root == null) asset.RootNodeId = node.Id;
            Dirty(asset);
            return node;
        }

        public static void DeleteNodes(AIGraphAsset asset, ICollection<string> nodeIds)
        {
            if (asset == null || nodeIds == null || nodeIds.Count == 0) return;
            Record(asset, "Delete AI Nodes");
            foreach (string id in nodeIds)
            {
                DetachFromParents(asset, id);
                asset.RemoveNode(id);
            }
            foreach (AIGraphGroup group in asset.Groups) group.NodeIds.RemoveAll(nodeIds.Contains);
            if (!string.IsNullOrEmpty(asset.RootNodeId) && nodeIds.Contains(asset.RootNodeId)) asset.RootNodeId = null;
            Dirty(asset);
        }

        public static bool CanConnect(AIGraphAsset asset, string parentId, string childId)
        {
            if (asset == null || string.IsNullOrEmpty(parentId) || string.IsNullOrEmpty(childId)) return false;
            AINode parent = asset.GetNode(parentId);
            if (!(parent is CompositeNode) && !(parent is DecoratorNode)) return false;
            if (asset.GetNode(childId) == null || childId == asset.RootNodeId) return false;
            return !WouldCreateCycle(asset, parentId, childId);
        }

        public static bool Connect(AIGraphAsset asset, string parentId, string childId)
        {
            if (!CanConnect(asset, parentId, childId)) return false;
            Record(asset, "Connect AI Nodes");
            DetachFromParents(asset, childId);
            AINode parent = asset.GetNode(parentId);
            if (parent is DecoratorNode decorator)
            {
                decorator.ChildId = childId;
            }
            else if (parent is CompositeNode composite)
            {
                composite.AddChild(childId);
                SortChildren(asset, composite);
            }
            Dirty(asset);
            return true;
        }

        public static void Disconnect(AIGraphAsset asset, string parentId, string childId)
        {
            AINode parent = asset != null ? asset.GetNode(parentId) : null;
            if (parent == null) return;
            Record(asset, "Disconnect AI Nodes");
            if (parent is CompositeNode composite)
            {
                while (composite.RemoveChild(childId)) { }
            }
            else if (parent is DecoratorNode decorator && decorator.ChildId == childId)
            {
                decorator.ChildId = null;
            }
            Dirty(asset);
        }

        public static void SetRoot(AIGraphAsset asset, string nodeId)
        {
            if (asset == null || asset.GetNode(nodeId) == null) return;
            Record(asset, "Set AI Root");
            DetachFromParents(asset, nodeId);
            asset.RootNodeId = nodeId;
            Dirty(asset);
        }

        public static void MoveNodes(AIGraphAsset asset, IReadOnlyDictionary<string, Vector2> positions)
        {
            if (asset == null || positions == null || positions.Count == 0) return;
            Record(asset, "Move AI Nodes");
            foreach (KeyValuePair<string, Vector2> pair in positions)
            {
                AINode node = asset.GetNode(pair.Key);
                if (node != null) node.EditorPosition = pair.Value;
            }
            foreach (AINode node in asset.Nodes)
                if (node is CompositeNode composite) SortChildren(asset, composite);
            Dirty(asset);
        }

        public static string ParentIdOf(AIGraphAsset asset, string childId)
        {
            if (asset == null) return null;
            foreach (AINode node in asset.Nodes)
            {
                if (node == null) continue;
                IReadOnlyList<string> ids = AIGraphAsset.ChildIdsOf(node);
                for (int i = 0; i < ids.Count; i++)
                    if (ids[i] == childId) return node.Id;
            }
            return null;
        }

        /// <summary>True when linking parent to child would close a loop (child already reaches parent, or they are the same node).</summary>
        public static bool WouldCreateCycle(AIGraphAsset asset, string parentId, string childId)
        {
            if (parentId == childId) return true;
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(childId);
            while (stack.Count > 0)
            {
                string id = stack.Pop();
                if (id == parentId) return true;
                if (!visited.Add(id)) continue;
                AINode node = asset.GetNode(id);
                if (node == null) continue;
                IReadOnlyList<string> ids = AIGraphAsset.ChildIdsOf(node);
                for (int i = 0; i < ids.Count; i++) stack.Push(ids[i]);
            }
            return false;
        }

        public static AIGraphGroup CreateGroup(AIGraphAsset asset, string title, Vector2 position, IEnumerable<string> nodeIds)
        {
            Record(asset, "Create AI Group");
            var group = new AIGraphGroup { Title = title, Position = position };
            asset.Groups.Add(group);
            AddIds(asset, group, nodeIds);
            Dirty(asset);
            return group;
        }

        public static void RemoveGroup(AIGraphAsset asset, string groupId)
        {
            if (FindGroup(asset, groupId) == null) return;
            Record(asset, "Delete AI Group");
            asset.Groups.RemoveAll(g => g.Id == groupId);
            Dirty(asset);
        }

        public static void SetGroupTitle(AIGraphAsset asset, string groupId, string title)
        {
            AIGraphGroup group = FindGroup(asset, groupId);
            if (group == null) return;
            Record(asset, "Rename AI Group");
            group.Title = title;
            Dirty(asset);
        }

        public static void SetGroupPosition(AIGraphAsset asset, string groupId, Vector2 position)
        {
            AIGraphGroup group = FindGroup(asset, groupId);
            if (group == null) return;
            Record(asset, "Move AI Group");
            group.Position = position;
            Dirty(asset);
        }

        public static void AddToGroup(AIGraphAsset asset, string groupId, IEnumerable<string> nodeIds)
        {
            AIGraphGroup group = FindGroup(asset, groupId);
            if (group == null) return;
            Record(asset, "Add To AI Group");
            AddIds(asset, group, nodeIds);
            Dirty(asset);
        }

        public static void RemoveFromGroup(AIGraphAsset asset, string groupId, IEnumerable<string> nodeIds)
        {
            AIGraphGroup group = FindGroup(asset, groupId);
            if (group == null || nodeIds == null) return;
            Record(asset, "Remove From AI Group");
            foreach (string id in nodeIds) group.NodeIds.Remove(id);
            Dirty(asset);
        }

        public static AIGraphNote CreateNote(AIGraphAsset asset, Rect rect)
        {
            Record(asset, "Create AI Note");
            var note = new AIGraphNote { Position = rect };
            asset.Notes.Add(note);
            Dirty(asset);
            return note;
        }

        public static void RemoveNote(AIGraphAsset asset, string noteId)
        {
            if (FindNote(asset, noteId) == null) return;
            Record(asset, "Delete AI Note");
            asset.Notes.RemoveAll(n => n.Id == noteId);
            Dirty(asset);
        }

        public static void UpdateNote(AIGraphAsset asset, string noteId, string title, string contents, Rect rect)
        {
            AIGraphNote note = FindNote(asset, noteId);
            if (note == null) return;
            if (note.Title == title && note.Contents == contents && note.Position == rect) return;
            Record(asset, "Edit AI Note");
            note.Title = title;
            note.Contents = contents;
            note.Position = rect;
            Dirty(asset);
        }

        static void SortChildren(AIGraphAsset asset, CompositeNode composite)
        {
            IReadOnlyList<string> ids = composite.ChildIds;
            if (ids.Count < 2) return;
            var entries = new List<(string id, float x, int index)>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                AINode child = asset.GetNode(ids[i]);
                // A missing child (deleted class) has no known position; re-sorting would rewrite saved order.
                if (child == null) return;
                entries.Add((ids[i], child.EditorPosition.x, i));
            }
            entries.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.index.CompareTo(b.index));
            bool changed = false;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].id != ids[i]) changed = true;
            if (!changed) return;
            for (int i = 0; i < entries.Count; i++) composite.RemoveChild(entries[i].id);
            for (int i = 0; i < entries.Count; i++) composite.AddChild(entries[i].id);
        }

        static void DetachFromParents(AIGraphAsset asset, string childId)
        {
            foreach (AINode node in asset.Nodes)
            {
                if (node is CompositeNode composite)
                {
                    while (composite.RemoveChild(childId)) { }
                }
                else if (node is DecoratorNode decorator && decorator.ChildId == childId)
                {
                    decorator.ChildId = null;
                }
            }
        }

        static void AddIds(AIGraphAsset asset, AIGraphGroup target, IEnumerable<string> nodeIds)
        {
            if (nodeIds == null) return;
            foreach (string id in nodeIds)
            {
                foreach (AIGraphGroup group in asset.Groups)
                    if (group != target) group.NodeIds.Remove(id);
                if (!target.NodeIds.Contains(id)) target.NodeIds.Add(id);
            }
        }

        static AIGraphGroup FindGroup(AIGraphAsset asset, string groupId)
        {
            if (asset == null) return null;
            foreach (AIGraphGroup group in asset.Groups)
                if (group.Id == groupId) return group;
            return null;
        }

        static AIGraphNote FindNote(AIGraphAsset asset, string noteId)
        {
            if (asset == null) return null;
            foreach (AIGraphNote note in asset.Notes)
                if (note.Id == noteId) return note;
            return null;
        }

        static void Record(AIGraphAsset asset, string label) => Undo.RecordObject(asset, label);

        static void Dirty(AIGraphAsset asset) => EditorUtility.SetDirty(asset);
    }
}

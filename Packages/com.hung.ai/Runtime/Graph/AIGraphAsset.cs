using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>Behavior-tree graph. Nodes link by child id. Agents tick a runtime copy, never the asset.</summary>
    [CreateAssetMenu(menuName = "Hung/AI/AI Graph", fileName = "NewAIGraph")]
    public sealed class AIGraphAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeReference] List<AINode> nodes = new List<AINode>();
        [SerializeField] string rootNodeId;
        [SerializeField] BlackboardDefinition blackboard = new BlackboardDefinition();
        [SerializeField] List<AIGraphGroup> groups = new List<AIGraphGroup>();
        [SerializeField] List<AIGraphNote> notes = new List<AIGraphNote>();
        [NonSerialized] Dictionary<string, AINode> lookup;

        public IReadOnlyList<AINode> Nodes => nodes;

        public string RootNodeId
        {
            get => rootNodeId;
            set => rootNodeId = value;
        }

        public AINode Root => GetNode(rootNodeId);

        public BlackboardDefinition Blackboard => blackboard ??= new BlackboardDefinition();

        /// <summary>Editor layout only. The runtime never reads it.</summary>
        public List<AIGraphGroup> Groups => groups ??= new List<AIGraphGroup>();

        /// <summary>Editor layout only. The runtime never reads it.</summary>
        public List<AIGraphNote> Notes => notes ??= new List<AIGraphNote>();

        public T AddNode<T>(T node) where T : AINode
        {
            nodes.Add(node);
            lookup = null;
            return node;
        }

        public bool RemoveNode(string id)
        {
            bool removed = nodes.RemoveAll(n => n != null && n.Id == id) > 0;
            lookup = null;
            return removed;
        }

        public AINode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out AINode node) ? node : null;
        }

        /// <summary>Clones the asset (including node objects) and binds child references. Allocates; call once per agent.</summary>
        public AIGraphAsset CreateRuntimeCopy()
        {
            AIGraphAsset copy = Instantiate(this);
            copy.name = name + " (Runtime)";
            copy.lookup = null;
            for (int i = 0; i < copy.nodes.Count; i++) copy.nodes[i]?.Bind(copy);
            return copy;
        }

        public void Validate(List<string> errors)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                AINode n = nodes[i];
                if (n == null)
                {
                    errors.Add($"Node at index {i} has a missing type (class deleted or renamed without [MovedFrom]).");
                    continue;
                }
                if (!seen.Add(n.Id)) errors.Add($"Duplicate node id '{n.Id}' ({n.Title}).");
            }
            if (Root == null) errors.Add("Graph has no root node.");
            for (int i = 0; i < nodes.Count; i++)
            {
                AINode n = nodes[i];
                if (n == null) continue;
                IReadOnlyList<string> ids = ChildIdsOf(n);
                for (int c = 0; c < ids.Count; c++)
                    if (GetNode(ids[c]) == null) errors.Add($"'{n.Title}' references missing child '{ids[c]}'.");
                if (n is DecoratorNode d && string.IsNullOrEmpty(d.ChildId)) errors.Add($"'{n.Title}' has no child.");
            }
            if (HasCycle(out string cycleAt)) errors.Add($"Graph has a cycle at '{cycleAt}'.");
        }

        public bool HasCycle(out string nodeTitle)
        {
            var state = new Dictionary<string, int>();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null && Visit(nodes[i], state, out nodeTitle)) return true;
            nodeTitle = null;
            return false;
        }

        public static IReadOnlyList<string> ChildIdsOf(AINode node)
        {
            if (node is CompositeNode c) return c.ChildIds;
            if (node is DecoratorNode d && !string.IsNullOrEmpty(d.ChildId)) return new[] { d.ChildId };
            return Array.Empty<string>();
        }

        bool Visit(AINode node, Dictionary<string, int> state, out string nodeTitle)
        {
            nodeTitle = null;
            if (state.TryGetValue(node.Id, out int s))
            {
                if (s == 1)
                {
                    nodeTitle = node.Title;
                    return true;
                }
                return false;
            }
            state[node.Id] = 1;
            IReadOnlyList<string> ids = ChildIdsOf(node);
            for (int i = 0; i < ids.Count; i++)
            {
                AINode child = GetNode(ids[i]);
                if (child != null && Visit(child, state, out nodeTitle)) return true;
            }
            state[node.Id] = 2;
            return false;
        }

        /// <summary>Removes entries whose node class no longer exists (they deserialize as null). Returns how many were removed.</summary>
        public int RemoveMissingNodes()
        {
            int removed = nodes.RemoveAll(n => n == null);
            lookup = null;
            return removed;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Undo, inspector edits and Instantiate re-deserialize the node list; cached node objects may be stale.
            lookup = null;
        }

        void BuildLookup()
        {
            lookup = new Dictionary<string, AINode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                AINode n = nodes[i];
                if (n != null && !lookup.ContainsKey(n.Id)) lookup[n.Id] = n;
            }
        }
    }
}

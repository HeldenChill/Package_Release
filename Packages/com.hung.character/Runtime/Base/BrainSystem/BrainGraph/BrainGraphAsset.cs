using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    [CreateAssetMenu(fileName = "BrainGraph", menuName = "Gameplay/Character/Brain Graph", order = 100)]
    public sealed class BrainGraphAsset : ScriptableObject
    {
        [SerializeField] private int nextNodeId = 1;
        public int startNodeId = -1;
        public List<BrainGraphNode> nodes = new();
        public List<BrainGraphEdge> edges = new();

        [Header("Runtime Defaults")]
        public bool resetBrainDataBeforeTick = false;
        public float seenMemorySeconds = 6f;
        public float soundMemorySeconds = 6f;
        public float worldChangeMemorySeconds = 5f;

        public IReadOnlyList<BrainGraphNode> Nodes => nodes;
        public IReadOnlyList<BrainGraphEdge> Edges => edges;

        [System.NonSerialized] private Dictionary<int, BrainGraphNode> nodeMap;
        [System.NonSerialized] private Dictionary<int, List<BrainGraphEdge>> outgoingMap;
        private static readonly System.Comparison<BrainGraphEdge> EdgeOrderComparison = (a, b) => a.order.CompareTo(b.order);

        public void InvalidateRuntimeCache()
        {
            nodeMap = null;
            outgoingMap = null;
        }

        public void EnsureRuntimeCache()
        {
            if (nodeMap != null && outgoingMap != null) return;
            BuildRuntimeCache();
        }

        public void BuildRuntimeCache()
        {
            nodeMap = new Dictionary<int, BrainGraphNode>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                BrainGraphNode node = nodes[i];
                if (node != null) nodeMap[node.id] = node;
            }

            outgoingMap = new Dictionary<int, List<BrainGraphEdge>>();
            for (int i = 0; i < edges.Count; i++)
            {
                BrainGraphEdge edge = edges[i];
                if (edge == null || edge.disabled) continue;
                if (!outgoingMap.TryGetValue(edge.fromNodeId, out var list))
                {
                    list = new List<BrainGraphEdge>();
                    outgoingMap[edge.fromNodeId] = list;
                }
                list.Add(edge);
            }

            foreach (var kvp in outgoingMap)
            {
                kvp.Value.Sort(EdgeOrderComparison);
            }
        }

        public BrainGraphNode GetNode(int id)
        {
            if (nodeMap != null && nodeMap.TryGetValue(id, out var cachedNode))
                return cachedNode;

            for (int i = 0; i < nodes.Count; i++)
            {
                BrainGraphNode node = nodes[i];
                if (node != null && node.id == id) return node;
            }
            return null;
        }

        public List<BrainGraphEdge> GetOutgoing(int nodeId, List<BrainGraphEdge> buffer = null)
        {
            if (outgoingMap != null && outgoingMap.TryGetValue(nodeId, out var cachedList))
            {
                if (buffer == null) return cachedList;
                buffer.Clear();
                buffer.AddRange(cachedList);
                return buffer;
            }

            buffer ??= new List<BrainGraphEdge>();
            buffer.Clear();
            for (int i = 0; i < edges.Count; i++)
            {
                BrainGraphEdge edge = edges[i];
                if (edge == null || edge.disabled) continue;
                if (edge.fromNodeId == nodeId) buffer.Add(edge);
            }
            buffer.Sort(EdgeOrderComparison);
            return buffer;
        }

        public IReadOnlyList<BrainGraphEdge> GetOutgoingCached(int nodeId)
        {
            EnsureRuntimeCache();
            if (outgoingMap != null && outgoingMap.TryGetValue(nodeId, out var cachedList))
                return cachedList;
            return System.Array.Empty<BrainGraphEdge>();
        }

        public BrainGraphNode AddNode(BrainGraphNodeKind kind, Vector2 position)
        {
            BrainGraphNode node = new BrainGraphNode
            {
                id = nextNodeId++,
                kind = kind,
                title = MakeDefaultTitle(kind),
                editorPosition = position
            };
            nodes.Add(node);
            InvalidateRuntimeCache();
            if (startNodeId < 0 && kind == BrainGraphNodeKind.Root)
            {
                startNodeId = node.id;
            }
            return node;
        }

        public void AddEdge(int fromNodeId, int toNodeId, string label = null)
        {
            if (fromNodeId == toNodeId) return;
            if (GetNode(fromNodeId) == null || GetNode(toNodeId) == null) return;
            if (edges.Exists(e => e.fromNodeId == fromNodeId && e.toNodeId == toNodeId)) return;
            int order = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].fromNodeId == fromNodeId) order = Mathf.Max(order, edges[i].order + 1);
            }
            InvalidateRuntimeCache();
            edges.Add(new BrainGraphEdge { fromNodeId = fromNodeId, toNodeId = toNodeId, order = order, label = label });
        }

        public void RemoveNode(int id)
        {
            InvalidateRuntimeCache();
            nodes.RemoveAll(n => n.id == id);
            edges.RemoveAll(e => e.fromNodeId == id || e.toNodeId == id);
            if (startNodeId == id) startNodeId = nodes.Count > 0 ? nodes[0].id : -1;
        }

        public void RemoveEdge(BrainGraphEdge edge)
        {
            InvalidateRuntimeCache();
            edges.Remove(edge);
        }

        public void EnsureValidGraph()
        {
            nodes.RemoveAll(n => n == null);
            edges.RemoveAll(e => e == null || GetNode(e.fromNodeId) == null || GetNode(e.toNodeId) == null || e.fromNodeId == e.toNodeId);
            int maxId = 0;
            for (int i = 0; i < nodes.Count; i++) maxId = Mathf.Max(maxId, nodes[i].id);
            nextNodeId = Mathf.Max(nextNodeId, maxId + 1);
            if (startNodeId < 0 && nodes.Count > 0)
            {
                BrainGraphNode root = nodes.Find(n => n.kind == BrainGraphNodeKind.Root);
                startNodeId = root != null ? root.id : nodes[0].id;
            }
        }

        public void BuildDefaultEnemyGraph()
        {
            InvalidateRuntimeCache();
            nodes.Clear();
            edges.Clear();
            nextNodeId = 1;

            BrainGraphNode root = AddNode(BrainGraphNodeKind.Root, new Vector2(80, 180));
            BrainGraphNode selector = AddNode(BrainGraphNodeKind.Selector, new Vector2(360, 180));
            BrainGraphNode seeSeq = AddNode(BrainGraphNodeKind.Sequence, new Vector2(650, 40));
            seeSeq.title = "Seen target branch";
            BrainGraphNode perceptionA = AddNode(BrainGraphNodeKind.Perception, new Vector2(940, 40));
            BrainGraphNode see = AddNode(BrainGraphNodeKind.SeeSomething, new Vector2(1220, 40));
            see.floatValue = seenMemorySeconds;
            BrainGraphNode moveSeen = AddNode(BrainGraphNodeKind.MoveToTarget, new Vector2(1500, 40));
            moveSeen.targetSource = BrainGraphTargetSource.SeenEnemyPosition;

            BrainGraphNode hearSeq = AddNode(BrainGraphNodeKind.Sequence, new Vector2(650, 220));
            hearSeq.title = "Heard sound branch";
            BrainGraphNode perceptionB = AddNode(BrainGraphNodeKind.Perception, new Vector2(940, 220));
            BrainGraphNode hear = AddNode(BrainGraphNodeKind.HearSomething, new Vector2(1220, 220));
            hear.floatValue = soundMemorySeconds;
            BrainGraphNode moveSound = AddNode(BrainGraphNodeKind.MoveToTarget, new Vector2(1500, 220));
            moveSound.targetSource = BrainGraphTargetSource.LastSoundPosition;

            BrainGraphNode idleSeq = AddNode(BrainGraphNodeKind.Sequence, new Vector2(650, 400));
            idleSeq.title = "Idle awareness branch";
            BrainGraphNode perceptionC = AddNode(BrainGraphNodeKind.Perception, new Vector2(940, 400));
            BrainGraphNode aware = AddNode(BrainGraphNodeKind.AwareAround, new Vector2(1220, 400));
            BrainGraphNode look = AddNode(BrainGraphNodeKind.LookAround, new Vector2(1500, 400));
            look.floatValue = 0.8f;

            AddEdge(root.id, selector.id, "tick");
            AddEdge(selector.id, seeSeq.id, "see");
            AddEdge(selector.id, hearSeq.id, "hear");
            AddEdge(selector.id, idleSeq.id, "idle");
            AddEdge(seeSeq.id, perceptionA.id);
            AddEdge(seeSeq.id, see.id);
            AddEdge(seeSeq.id, moveSeen.id);
            AddEdge(hearSeq.id, perceptionB.id);
            AddEdge(hearSeq.id, hear.id);
            AddEdge(hearSeq.id, moveSound.id);
            AddEdge(idleSeq.id, perceptionC.id);
            AddEdge(idleSeq.id, aware.id);
            AddEdge(idleSeq.id, look.id);
            startNodeId = root.id;
        }

        private static string MakeDefaultTitle(BrainGraphNodeKind kind)
        {
            switch (kind)
            {
                case BrainGraphNodeKind.Root: return "Root";
                case BrainGraphNodeKind.Sequence: return "Sequence";
                case BrainGraphNodeKind.Selector: return "Selector";
                case BrainGraphNodeKind.Parallel: return "Parallel";
                case BrainGraphNodeKind.Perception: return "Sense World";
                case BrainGraphNodeKind.SeeSomething: return "See Something";
                case BrainGraphNodeKind.HearSomething: return "Hear Something";
                case BrainGraphNodeKind.MoveToTarget: return "Move To Target";
                case BrainGraphNodeKind.AwareAround: return "Aware Around";
                default: return kind.ToString();
            }
        }
    }
}

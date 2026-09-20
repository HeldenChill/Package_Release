using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Gameplay.Character.WorldInterface;

namespace Gameplay.Character.BrainSystem
{
    public sealed class BrainGraphExecutionContext
    {
        public BrainGraphAsset Graph { get; }
        public BrainData Data { get; }
        public BrainParameter Parameter { get; }
        public BrainGraphModule Owner { get; }
        public NavMeshAgent NavMeshAgent { get; }
        public Transform Transform { get; }
        public BrainGraphRuntimeDebugState DebugState { get; }
        public bool DebugRuntime => Owner != null && Owner.DebugRuntime;

        public Vector3 LastSoundPosition;
        public Collider LastSeenCollider;
        public ulong LastSeenHash;
        public ulong LastSoundHash;
        public ulong LastWorldHash;
        public float SeenMemoryUntil;
        public float SoundMemoryUntil;
        public float WorldChangeMemoryUntil;
        public readonly Dictionary<int, float> NodeStartedAt = new();
        public readonly HashSet<int> VisitedThisTick = new();
        public readonly HashSet<int> VisitedEver = new();
        public readonly List<BrainGraphEdge> EdgeBuffer = new();

        public BrainGraphExecutionContext(BrainGraphAsset graph, BrainGraphModule owner, BrainData data, BrainParameter parameter, NavMeshAgent navMeshAgent)
        {
            Graph = graph;
            Owner = owner;
            Data = data;
            Parameter = parameter;
            NavMeshAgent = navMeshAgent;
            Transform = owner != null ? owner.transform : null;
            DebugState = new BrainGraphRuntimeDebugState();
        }

        public void BeginTick()
        {
            VisitedThisTick.Clear();
            DebugState.previousNodeId = DebugState.currentNodeId;
            DebugState.currentNodeId = -1;
            DebugState.activeLeafId = -1;
            DebugState.tickVersion++;
            DebugState.lastTickTime = Time.timeAsDouble;

            if (Graph != null && Graph.resetBrainDataBeforeTick)
            {
                Data.MoveDirection = Vector3.zero;
                Data.HasMoveTarget = false;
                Data.HasArrivedAtDestination = false;
                Data.RemainingDistance = 0f;
            }
        }

        public void MarkNode(BrainGraphNode node, BrainGraphStatus status)
        {
            if (node == null) return;
            DebugState.currentNodeId = node.id;
            DebugState.lastStatus = status;
            if (status == BrainGraphStatus.Success) DebugState.lastSuccessNodeId = node.id;
            if (status == BrainGraphStatus.Failure) DebugState.lastFailureNodeId = node.id;
            // Track the active *leaf* (action) node, not composites. Composites mark themselves
            // last (after children return), which would otherwise pin currentNodeId to the root.
            // Prefer a Running leaf; fall back to the last non-Failure leaf this tick.
            if (!IsComposite(node.kind) && status != BrainGraphStatus.Failure)
            {
                if (status == BrainGraphStatus.Running || DebugState.activeLeafId < 0)
                    DebugState.activeLeafId = node.id;
            }
            VisitedThisTick.Add(node.id);
            VisitedEver.Add(node.id);
        }

        private static bool IsComposite(BrainGraphNodeKind kind)
        {
            return kind == BrainGraphNodeKind.Root
                || kind == BrainGraphNodeKind.Sequence
                || kind == BrainGraphNodeKind.Selector
                || kind == BrainGraphNodeKind.Parallel
                || kind == BrainGraphNodeKind.Comment;
        }

        public void SetMessage(string message)
        {
            DebugState.lastMessage = message;
        }

        public void StartNodeTimerIfNeeded(int nodeId)
        {
            if (!NodeStartedAt.ContainsKey(nodeId)) NodeStartedAt[nodeId] = Time.time;
        }

        public void ResetNodeTimer(int nodeId)
        {
            NodeStartedAt.Remove(nodeId);
        }

        public bool HasNodeDurationPassed(int nodeId, float duration)
        {
            StartNodeTimerIfNeeded(nodeId);
            return Time.time - NodeStartedAt[nodeId] >= Mathf.Max(0f, duration);
        }

        public Vector3 ResolveTarget(BrainGraphNode node)
        {
            switch (node.targetSource)
            {
                case BrainGraphTargetSource.MoveTarget:
                    return Data.MoveTarget;
                case BrainGraphTargetSource.LastSoundPosition:
                    return LastSoundPosition;
                case BrainGraphTargetSource.ManualPosition:
                    return node.vectorValue;
                case BrainGraphTargetSource.CharacterForward:
                    return Transform != null ? Transform.position + Transform.forward * Mathf.Max(0.1f, node.floatValue) : node.vectorValue;
                case BrainGraphTargetSource.SeenEnemyPosition:
                default:
                    return Data.SeenEnemyPosition;
            }
        }

        public bool HasWorldInterfaceData()
        {
            return Parameter != null && Parameter.WIData != null;
        }

        public WorldInterfaceData GetWorldInterfaceData()
        {
            return Parameter != null ? Parameter.WIData : null;
        }
    }
}

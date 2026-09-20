using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Character.BrainSystem
{
    public sealed class BrainGraphRunner
    {
        private readonly BrainGraphAsset graph;
        private readonly BrainGraphExecutionContext context;
        private readonly List<BrainGraphEdge> outgoingBuffer = new();

        public BrainGraphRuntimeDebugState DebugState => context.DebugState;
        public IReadOnlyCollection<int> VisitedThisTick => context.VisitedThisTick;
        public IReadOnlyCollection<int> VisitedEver => context.VisitedEver;
        public BrainGraphExecutionContext Context => context;

        public BrainGraphRunner(BrainGraphAsset graph, BrainGraphModule owner, BrainData data, BrainParameter parameter, NavMeshAgent navMeshAgent)
        {
            this.graph = graph;
            this.graph?.EnsureValidGraph();
            this.graph?.EnsureRuntimeCache();
            context = new BrainGraphExecutionContext(graph, owner, data, parameter, navMeshAgent);
        }

        public BrainGraphStatus Tick()
        {
            if (graph == null)
            {
                context.SetMessage("BrainGraph is missing.");
                return BrainGraphStatus.Failure;
            }

            context.BeginTick();
            BrainGraphNode start = graph.GetNode(graph.startNodeId);
            if (start == null)
            {
                context.SetMessage("Start node is missing.");
                return BrainGraphStatus.Failure;
            }

            BrainGraphStatus status = TickNode(start);
            context.DebugState.lastStatus = status;
            return status;
        }

        private BrainGraphStatus TickNode(BrainGraphNode node)
        {
            if (node == null || node.disabled)
            {
                return BrainGraphStatus.Failure;
            }

            BrainGraphStatus result;
            switch (node.kind)
            {
                case BrainGraphNodeKind.Root:
                    result = TickSequenceChildren(node);
                    break;
                case BrainGraphNodeKind.Sequence:
                    result = TickSequenceChildren(node);
                    break;
                case BrainGraphNodeKind.Selector:
                    result = TickSelectorChildren(node);
                    break;
                case BrainGraphNodeKind.Parallel:
                    result = TickParallelChildren(node);
                    break;
                case BrainGraphNodeKind.Comment:
                    result = BrainGraphStatus.Success;
                    break;
                default:
                    result = BrainGraphNodeRegistry.Tick(node, context);
                    break;
            }

            context.MarkNode(node, result);
            return result;
        }

        private BrainGraphStatus TickSequenceChildren(BrainGraphNode node)
        {
            var outgoing = graph.GetOutgoingCached(node.id);
            if (outgoing.Count == 0) return BrainGraphStatus.Success;

            for (int i = 0; i < outgoing.Count; i++)
            {
                BrainGraphNode child = graph.GetNode(outgoing[i].toNodeId);
                BrainGraphStatus childStatus = TickNode(child);
                if (childStatus == BrainGraphStatus.Failure) return BrainGraphStatus.Failure;
                if (childStatus == BrainGraphStatus.Running) return BrainGraphStatus.Running;
            }
            return BrainGraphStatus.Success;
        }

        private BrainGraphStatus TickSelectorChildren(BrainGraphNode node)
        {
            var outgoing = graph.GetOutgoingCached(node.id);
            if (outgoing.Count == 0) return BrainGraphStatus.Failure;

            bool hasRunning = false;
            for (int i = 0; i < outgoing.Count; i++)
            {
                BrainGraphNode child = graph.GetNode(outgoing[i].toNodeId);
                BrainGraphStatus childStatus = TickNode(child);
                if (childStatus == BrainGraphStatus.Success) return BrainGraphStatus.Success;
                if (childStatus == BrainGraphStatus.Running) hasRunning = true;
            }
            return hasRunning ? BrainGraphStatus.Running : BrainGraphStatus.Failure;
        }

        private BrainGraphStatus TickParallelChildren(BrainGraphNode node)
        {
            var outgoing = graph.GetOutgoingCached(node.id);
            if (outgoing.Count == 0) return BrainGraphStatus.Success;

            bool anyRunning = false;
            for (int i = 0; i < outgoing.Count; i++)
            {
                BrainGraphNode child = graph.GetNode(outgoing[i].toNodeId);
                BrainGraphStatus childStatus = TickNode(child);
                if (childStatus == BrainGraphStatus.Failure) return BrainGraphStatus.Failure;
                if (childStatus == BrainGraphStatus.Running) anyRunning = true;
            }
            return anyRunning ? BrainGraphStatus.Running : BrainGraphStatus.Success;
        }
    }
}

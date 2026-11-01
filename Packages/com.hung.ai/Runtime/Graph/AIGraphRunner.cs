using UnityEngine;
namespace Hung.AI
{
    /// <summary>Ticks a runtime graph copy. When the root finishes, the next tick starts it again.</summary>
    public sealed class AIGraphRunner
    {
        readonly bool isBroken;

        public AIGraphRunner(AIGraphAsset runtimeGraph, AIContext context)
        {
            Graph = runtimeGraph;
            Context = context;
            if (runtimeGraph != null && runtimeGraph.HasCycle(out string at))
            {
                isBroken = true;
                Debug.LogWarning($"[Hung.AI] Graph '{runtimeGraph.name}' has a cycle at '{at}'. The agent does not tick.");
            }
        }

        public AIGraphAsset Graph { get; }

        public AIContext Context { get; }

        public NodeStatus LastStatus { get; private set; }

        public NodeStatus Tick(float deltaTime)
        {
            Context.DeltaTime = deltaTime;
            Context.Time += deltaTime;
            if (isBroken) return LastStatus = NodeStatus.Failure;
            AINode root = Graph != null ? Graph.Root : null;
            if (root == null)
            {
                Context.LogOnce("root", $"[Hung.AI] Graph '{(Graph != null ? Graph.name : "null")}' has no root node. The agent does not tick.");
                return LastStatus = NodeStatus.Failure;
            }
            return LastStatus = root.Tick(Context);
        }

        /// <summary>Aborts the active path. The next tick starts from the root.</summary>
        public void Abort()
        {
            if (Graph != null) Graph.Root?.Abort(Context);
        }
    }
}

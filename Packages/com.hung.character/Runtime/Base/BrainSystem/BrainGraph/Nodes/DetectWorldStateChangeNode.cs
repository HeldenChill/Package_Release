using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class DetectWorldStateChangeNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.DetectWorldStateChange;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            float memory = node.floatValue > 0f ? node.floatValue : context.Graph.worldChangeMemorySeconds;
            ulong hash = ENVIRONMENT_HASH.Compute(
                context.Parameter.SoundPositions,
                context.Parameter.SoundWeights,
                context.Parameter.SeenPositions,
                context.Parameter.SeenWeights);

            if (hash != context.LastWorldHash)
            {
                context.LastWorldHash = hash;
                context.WorldChangeMemoryUntil = Time.time + memory;
                context.SetMessage("World state changed.");
                return BrainGraphStatus.Success;
            }

            bool stillInMemory = Time.time <= context.WorldChangeMemoryUntil;
            context.SetMessage(stillInMemory ? "World state change memory." : "No world state change.");
            return stillInMemory ? BrainGraphStatus.Success : BrainGraphStatus.Failure;
        }
    }
}

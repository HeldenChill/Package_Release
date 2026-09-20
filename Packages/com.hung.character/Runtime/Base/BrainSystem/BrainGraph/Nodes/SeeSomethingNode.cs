using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class SeeSomethingNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.SeeSomething;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            float memory = node.floatValue > 0f ? node.floatValue : context.Graph.seenMemorySeconds;
            ulong seenHash = ENVIRONMENT_HASH.Compute(context.Parameter.SeenPositions, context.Parameter.SeenWeights);
            bool hasNewSeen = seenHash != 0 && seenHash != context.LastSeenHash;
            bool hasVisibleSeen = context.Parameter.SeenPositions.Count > 0;
            bool hasMemory = Time.time <= context.SeenMemoryUntil;

            if (hasNewSeen || hasVisibleSeen)
            {
                context.LastSeenHash = seenHash;
                context.SeenMemoryUntil = Time.time + memory;
                BrainGraphNodeHelpers.UpdateSeenTarget(context);
                context.Data.IsSeenEnemyCollider = context.LastSeenCollider != null;
                context.Data.IsSeePosition = true;
                context.SetMessage("See target.");
                return BrainGraphStatus.Success;
            }

            if (hasMemory)
            {
                context.Data.IsSeenEnemyCollider = context.LastSeenCollider != null;
                context.Data.IsSeePosition = true;
                context.SetMessage("See target from memory.");
                return BrainGraphStatus.Success;
            }

            context.Data.IsSeenEnemyCollider = false;
            context.SetMessage("See target failed.");
            return BrainGraphStatus.Failure;
        }
    }
}

using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class KeepTrackOfTargetNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.KeepTrackOfTarget;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            if (context.LastSeenCollider == null)
            {
                context.SetMessage("No collider target to track.");
                return BrainGraphStatus.Failure;
            }

            Vector3 position = context.LastSeenCollider.transform.position + Vector3.up * 0.5f;
            context.Data.SeenEnemyPosition = position;
            context.Data.IsSeePosition = true;
            context.Data.IsSeenEnemyCollider = true;
            context.SetMessage("Keep tracking seen target.");
            return BrainGraphStatus.Success;
        }
    }
}

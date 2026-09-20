using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class KeepTrackOfPositionNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.KeepTrackOfPosition;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            Vector3 position = node.targetSource == BrainGraphTargetSource.ManualPosition
                ? node.vectorValue
                : context.ResolveTarget(node);
            if (position == Vector3.zero && node.targetSource != BrainGraphTargetSource.ManualPosition)
            {
                context.SetMessage("No position to track.");
                return BrainGraphStatus.Failure;
            }

            Transform head = context.Parameter.PerceptionData != null ? context.Parameter.PerceptionData.HeadTf : null;
            if (head != null) position.y = head.position.y;
            context.Data.SeenEnemyPosition = position;
            context.Data.IsSeePosition = true;
            context.SetMessage("Keep tracking position.");
            return BrainGraphStatus.Success;
        }
    }
}

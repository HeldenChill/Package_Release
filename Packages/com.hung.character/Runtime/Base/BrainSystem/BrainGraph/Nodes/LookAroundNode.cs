using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class LookAroundNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.LookAround;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            context.Data.MoveDirection = Vector3.zero;
            if (context.HasNodeDurationPassed(node.id, Mathf.Max(0.1f, node.floatValue)))
            {
                context.ResetNodeTimer(node.id);
                context.SetMessage("Look around finished.");
                return BrainGraphStatus.Success;
            }
            context.SetMessage("Look around.");
            return BrainGraphStatus.Running;
        }
    }
}

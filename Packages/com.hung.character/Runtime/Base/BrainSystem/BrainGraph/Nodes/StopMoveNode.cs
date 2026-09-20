using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class StopMoveNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.StopMove;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            context.Data.MoveDirection = Vector3.zero;
            context.Data.HasMoveTarget = false;
            context.SetMessage("Stop move.");
            return BrainGraphStatus.Success;
        }
    }
}

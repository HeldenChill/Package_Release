using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class AwareAroundNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.AwareAround;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            context.Data.MoveDirection = Vector3.zero;
            context.Data.HasMoveTarget = false;
            context.SetMessage("Aware around.");
            return BrainGraphStatus.Success;
        }
    }
}

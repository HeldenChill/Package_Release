namespace Gameplay.Character.BrainSystem
{
    public sealed class SetBrainFlagNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.SetBrainFlag;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            if (node.writeMoveTarget)
            {
                context.Data.MoveTarget = node.vectorValue;
                context.Data.HasMoveTarget = true;
            }
            if (node.writeSeePosition)
            {
                context.Data.SeenEnemyPosition = node.vectorValue;
                context.Data.IsSeePosition = true;
            }
            if (node.writeRun) context.Data.IsRun = true;
            if (node.writeCrouch) context.Data.IsCrouch = true;
            if (node.intValue != 0) context.Data.PhaseId = node.intValue;
            context.SetMessage("Set BrainData flags.");
            return BrainGraphStatus.Success;
        }
    }
}

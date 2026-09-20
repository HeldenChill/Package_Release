namespace Gameplay.Character.BrainSystem
{
    public sealed class LoseTrackOfSeeTargetNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.LoseTrackOfSeeTarget;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            context.LastSeenCollider = null;
            context.LastSeenHash = 0;
            context.SeenMemoryUntil = 0f;
            context.Data.IsSeenEnemyCollider = false;
            context.Data.IsSeePosition = false;
            context.SetMessage("Lose seen target.");
            return BrainGraphStatus.Success;
        }
    }
}

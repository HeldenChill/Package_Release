namespace Gameplay.Character.BrainSystem
{
    // One isolated script per leaf node kind. Implement this, give it a [BrainGraphNode] enum kind,
    // and the registry auto-discovers it — no central switch to edit. Composites (Root/Sequence/
    // Selector/Parallel/Comment) are NOT handlers; they live in BrainGraphRunner.
    public interface IBrainGraphNode
    {
        BrainGraphNodeKind Kind { get; }
        BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context);
    }
}

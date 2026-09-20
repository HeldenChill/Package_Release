using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class DebugLogNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.DebugLog;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            Debug.Log(node.stringValue);
            context.SetMessage(node.stringValue);
            return BrainGraphStatus.Success;
        }
    }
}

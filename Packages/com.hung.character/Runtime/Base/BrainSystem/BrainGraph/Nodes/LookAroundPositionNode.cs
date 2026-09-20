using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class LookAroundPositionNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.LookAroundPosition;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            context.Data.MoveDirection = Vector3.zero;
            Vector3 target = context.ResolveTarget(node);
            Vector3 dir = target - context.Transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
                context.Transform.rotation = Quaternion.RotateTowards(context.Transform.rotation, targetRotation, 360f * Time.deltaTime);
            }

            if (context.HasNodeDurationPassed(node.id, Mathf.Max(0.1f, node.floatValue)))
            {
                context.ResetNodeTimer(node.id);
                context.SetMessage("Look around position finished.");
                return BrainGraphStatus.Success;
            }
            context.SetMessage("Look around position.");
            return BrainGraphStatus.Running;
        }
    }
}

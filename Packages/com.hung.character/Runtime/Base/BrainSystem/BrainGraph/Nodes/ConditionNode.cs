using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public sealed class ConditionNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.Condition;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            bool result = Evaluate(node, context);
            if (node.invertCondition) result = !result;
#if UNITY_EDITOR
            if (context.DebugRuntime)
            {
                context.SetMessage(result ? "Condition: Success" : "Condition: Failure");
            }
#endif
            return result ? BrainGraphStatus.Success : BrainGraphStatus.Failure;
        }

        private static bool Evaluate(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            switch (node.condition)
            {
                case BrainGraphConditionKind.HasSeenTarget:
                    return context.Data.IsSeenEnemyCollider || context.Data.IsSeePosition || Time.time <= context.SeenMemoryUntil;
                case BrainGraphConditionKind.HasSound:
                    return context.Parameter.SoundPositions.Count > 0 || Time.time <= context.SoundMemoryUntil;
                case BrainGraphConditionKind.HasWorldStateChange:
                    return Time.time <= context.WorldChangeMemoryUntil;
                case BrainGraphConditionKind.HasMoveTarget:
                    return context.Data.HasMoveTarget;
                case BrainGraphConditionKind.HasArrived:
                    return context.Data.HasArrivedAtDestination;
                case BrainGraphConditionKind.IsRunning:
                    return context.Data.IsRun;
                case BrainGraphConditionKind.IsCrouching:
                    return context.Data.IsCrouch;
                case BrainGraphConditionKind.CustomPhaseIdEquals:
                    return context.Data.PhaseId == node.intValue;
                case BrainGraphConditionKind.Always:
                default:
                    return true;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Character.BrainSystem
{
    public sealed class MoveToTargetNode : IBrainGraphNode
    {
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.MoveToTarget;

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            if (context.Transform == null)
            {
                context.SetMessage("Move failed: owner transform is null.");
                return BrainGraphStatus.Failure;
            }

            Vector3 target = context.ResolveTarget(node);
            context.Data.MoveTarget = target;
            context.Data.HasMoveTarget = true;

            NavMeshAgent agent = context.NavMeshAgent;
            if (agent == null)
            {
                Vector3 direct = target - context.Transform.position;
                direct.y = 0f;
                if (direct.sqrMagnitude <= 0.04f)
                {
                    context.Data.MoveDirection = Vector3.zero;
                    context.Data.HasArrivedAtDestination = true;
                    context.SetMessage("Move reached target without NavMeshAgent.");
                    return BrainGraphStatus.Success;
                }

                context.Data.MoveDirection = direct.normalized;
                context.Data.HasArrivedAtDestination = false;
                context.SetMessage("Move direct without NavMeshAgent.");
                return BrainGraphStatus.Running;
            }

            if (!agent.enabled || !agent.isOnNavMesh)
            {
                context.SetMessage("Move failed: NavMeshAgent is disabled or not on NavMesh.");
                return BrainGraphStatus.Failure;
            }

            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.nextPosition = context.Transform.position;
            agent.SetDestination(target);

            Vector3 desired = agent.desiredVelocity;
            desired.y = 0f;
            Vector3 desiredDir = BrainGraphNodeHelpers.GetVelocity(context, agent, desired, node.id);
            bool arrived = BrainGraphNodeHelpers.HasReachedDestination(context, agent, desiredDir);
            context.Data.RemainingDistance = agent.remainingDistance;
            context.Data.HasArrivedAtDestination = arrived;

            if (arrived)
            {
                context.Data.MoveDirection = Vector3.zero;
                context.ResetNodeTimer(node.id);
                context.SetMessage("Move reached target.");
                return BrainGraphStatus.Success;
            }

            context.Data.MoveDirection = desiredDir;
            context.SetMessage("Move to target.");
            return BrainGraphStatus.Running;
        }
    }
}

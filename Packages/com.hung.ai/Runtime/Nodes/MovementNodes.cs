using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    [Serializable, AINode("Movement/Move To", Description = "Moves toward a Vector3, Transform or GameObject key through the agent's IAIMover. Succeeds on arrival; always stops the mover when it ends.")]
    public sealed class MoveToNode : ActionNode
    {
        [SerializeField] BlackboardKeyRef target = new BlackboardKeyRef();
        [SerializeField, Min(0f)] float stopDistance = 0.5f;

        public BlackboardKeyRef Target
        {
            get => target;
            set => target = value;
        }

        public float StopDistance
        {
            get => stopDistance;
            set => stopDistance = value;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            IAIMover mover = context.Mover;
            if (mover == null)
            {
                context.LogOnce(Id + "/mover", $"[Hung.AI] '{Title}' needs an IAIMover on the agent. It is treated as Failure.");
                return NodeStatus.Failure;
            }
            if (target == null || !target.IsAssigned)
            {
                context.LogOnce(Id + "/target", $"[Hung.AI] '{Title}' has no target key. It is treated as Failure.");
                return NodeStatus.Failure;
            }
            if (!context.Blackboard.TryGetPosition(target, out Vector3 destination)) return NodeStatus.Failure;
            if (!mover.SetDestination(destination)) return NodeStatus.Failure;
            if (mover.HasArrived || mover.RemainingDistance <= stopDistance) return NodeStatus.Success;
            return NodeStatus.Running;
        }

        protected override void OnEnd(AIContext context, NodeStatus status)
        {
            if (status == NodeStatus.Success && context.Mover is IAIMovementCompletion completion)
                completion.CompleteMovement();
            else
                context.Mover?.Stop();
        }
    }

    [Serializable, AINode("Movement/Stop Move", Description = "Stops the agent's IAIMover and succeeds.")]
    public sealed class StopMoveNode : ActionNode
    {
        protected override NodeStatus OnUpdate(AIContext context)
        {
            context.Mover?.Stop();
            return NodeStatus.Success;
        }
    }

    [Serializable, AINode("Movement/Patrol", Description = "Visits world-space points in order. Loops forever, or succeeds after the last point when Loop is off.")]
    public sealed class PatrolNode : ActionNode
    {
        [SerializeField] List<Vector3> points = new List<Vector3>();
        [SerializeField, Min(0f)] float stopDistance = 0.5f;
        [SerializeField] bool loop = true;
        [NonSerialized] int index;

        public List<Vector3> Points => points ??= new List<Vector3>();

        public float StopDistance
        {
            get => stopDistance;
            set => stopDistance = value;
        }

        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        protected override void OnStart(AIContext context)
        {
            index = 0;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            IAIMover mover = context.Mover;
            if (mover == null)
            {
                context.LogOnce(Id + "/mover", $"[Hung.AI] '{Title}' needs an IAIMover on the agent. It is treated as Failure.");
                return NodeStatus.Failure;
            }
            if (points == null || points.Count == 0)
            {
                context.LogOnce(Id + "/points", $"[Hung.AI] '{Title}' has no points. It is treated as Failure.");
                return NodeStatus.Failure;
            }
            if (!mover.SetDestination(points[index])) return NodeStatus.Failure;
            if (mover.HasArrived || mover.RemainingDistance <= stopDistance)
            {
                index++;
                if (index >= points.Count)
                {
                    if (!loop) return NodeStatus.Success;
                    index = 0;
                }
            }
            return NodeStatus.Running;
        }

        protected override void OnEnd(AIContext context, NodeStatus status)
        {
            if (status == NodeStatus.Success && context.Mover is IAIMovementCompletion completion)
                completion.CompleteMovement();
            else
                context.Mover?.Stop();
        }
    }
}

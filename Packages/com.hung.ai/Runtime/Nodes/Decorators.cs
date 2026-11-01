using System;
using UnityEngine;
namespace Hung.AI
{
    [Serializable, AINode("Decorators/Inverter", Description = "Success becomes Failure and Failure becomes Success.")]
    public sealed class InverterNode : DecoratorNode
    {
        protected override NodeStatus OnUpdate(AIContext context)
        {
            NodeStatus status = TickChild(context);
            if (status == NodeStatus.Success) return NodeStatus.Failure;
            if (status == NodeStatus.Failure) return NodeStatus.Success;
            return status;
        }
    }

    [Serializable, AINode("Decorators/Succeeder", Description = "Always succeeds once the child finishes.")]
    public sealed class SucceederNode : DecoratorNode
    {
        protected override NodeStatus OnUpdate(AIContext context)
        {
            return TickChild(context) == NodeStatus.Running ? NodeStatus.Running : NodeStatus.Success;
        }
    }

    [Serializable, AINode("Decorators/Repeater", Description = "Runs the child Count times (0 or less repeats forever). One child run per tick at most.")]
    public sealed class RepeaterNode : DecoratorNode
    {
        [SerializeField] int count = -1;
        [SerializeField] bool stopOnFailure = true;
        [NonSerialized] int completed;

        public int Count
        {
            get => count;
            set => count = value;
        }

        public bool StopOnFailure
        {
            get => stopOnFailure;
            set => stopOnFailure = value;
        }

        protected override void OnStart(AIContext context)
        {
            completed = 0;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            NodeStatus status = TickChild(context);
            if (status == NodeStatus.Running) return NodeStatus.Running;
            if (status == NodeStatus.Failure && stopOnFailure) return NodeStatus.Failure;
            completed++;
            if (count > 0 && completed >= count) return NodeStatus.Success;
            return NodeStatus.Running;
        }
    }

    [Serializable, AINode("Decorators/Until Success", Description = "Reruns the child until it succeeds.")]
    public sealed class UntilSuccessNode : DecoratorNode
    {
        protected override NodeStatus OnUpdate(AIContext context)
        {
            return TickChild(context) == NodeStatus.Success ? NodeStatus.Success : NodeStatus.Running;
        }
    }

    [Serializable, AINode("Decorators/Until Failure", Description = "Reruns the child until it fails, then succeeds.")]
    public sealed class UntilFailureNode : DecoratorNode
    {
        protected override NodeStatus OnUpdate(AIContext context)
        {
            return TickChild(context) == NodeStatus.Failure ? NodeStatus.Success : NodeStatus.Running;
        }
    }

    [Serializable, AINode("Decorators/Cooldown", Description = "After the child finishes, fails for Seconds before the child may run again.")]
    public sealed class CooldownNode : DecoratorNode
    {
        [SerializeField, Min(0f)] float seconds = 1f;
        [NonSerialized] float readyTime;

        public float Seconds
        {
            get => seconds;
            set => seconds = value;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            bool childRunning = child != null && child.IsRunning;
            if (!childRunning && context.Time < readyTime) return NodeStatus.Failure;
            NodeStatus status = TickChild(context);
            if (status != NodeStatus.Running) readyTime = context.Time + seconds;
            return status;
        }
    }

    [Serializable, AINode("Decorators/Time Limit", Description = "Aborts the child and fails when it runs longer than Seconds.")]
    public sealed class TimeLimitNode : DecoratorNode
    {
        [SerializeField, Min(0f)] float seconds = 5f;
        [NonSerialized] float startTime;

        public float Seconds
        {
            get => seconds;
            set => seconds = value;
        }

        protected override void OnStart(AIContext context)
        {
            startTime = context.Time;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            if (context.Time - startTime >= seconds)
            {
                child?.Abort(context);
                return NodeStatus.Failure;
            }
            return TickChild(context);
        }
    }
}

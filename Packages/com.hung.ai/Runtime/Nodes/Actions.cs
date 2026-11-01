using System;
using UnityEngine;
namespace Hung.AI
{
    [Serializable, AINode("Actions/Wait", Description = "Runs for Duration seconds plus up to RandomExtra seconds, then succeeds.")]
    public sealed class WaitNode : ActionNode
    {
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField, Min(0f)] float randomExtra;
        [NonSerialized] float endTime;

        public float Duration
        {
            get => duration;
            set => duration = value;
        }

        public float RandomExtra
        {
            get => randomExtra;
            set => randomExtra = value;
        }

        protected override void OnStart(AIContext context)
        {
            float extra = randomExtra > 0f ? (float)context.Random.NextDouble() * randomExtra : 0f;
            endTime = context.Time + duration + extra;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            return context.Time >= endTime ? NodeStatus.Success : NodeStatus.Running;
        }
    }

    [Serializable, AINode("Actions/Log", Description = "Writes Message to the Unity console and succeeds.")]
    public sealed class LogNode : ActionNode
    {
        [SerializeField] string message = "Log";

        public string Message
        {
            get => message;
            set => message = value;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            Debug.Log(message, context.Owner);
            return NodeStatus.Success;
        }
    }

    [Serializable, AINode("Actions/Set Blackboard", Description = "Writes a constant into a key. Fails if the key does not exist or the type does not match.")]
    public sealed class SetBlackboardNode : ActionNode
    {
        [SerializeField] BlackboardKeyRef key = new BlackboardKeyRef();
        [SerializeField] BlackboardValue value;

        public BlackboardKeyRef Key
        {
            get => key;
            set => key = value;
        }

        public BlackboardValue Value
        {
            get => value;
            set => this.value = value;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            return context.Blackboard.SetFromValue(key, value) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}

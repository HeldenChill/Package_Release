using System;
using Hung.AI;
using UnityEngine;
using BrainParameter = Gameplay.Character.BrainSystem.BrainParameter;
namespace Gameplay.Character.AIBridge
{
    internal static class CharacterAI
    {
        /// <summary>The AIBrainModule on the agent's GameObject, cached in the node's field. Null (with one warning) when there is none.</summary>
        public static AIBrainModule Resolve(AIContext context, AINode node, ref AIBrainModule cache)
        {
            if (cache != null) return cache;
            if (context.Owner != null && context.Owner.TryGetComponent(out AIBrainModule found)) return cache = found;
            context.LogOnce(node.Id + "/module", $"[Hung.AI] '{node.Title}' needs an AIBrainModule on the agent's GameObject. It is treated as Failure.");
            return null;
        }
    }

    [Serializable, AINode("Character/Perception/Sense World", Description = "Reads the character's sight and hearing sensors every tick and stays Running. Put it in a Parallel beside the decision branch.")]
    public sealed class SenseWorldNode : ActionNode
    {
        [NonSerialized] AIBrainModule module;

        protected override NodeStatus OnUpdate(AIContext context)
        {
            AIBrainModule m = CharacterAI.Resolve(context, this, ref module);
            if (m == null) return NodeStatus.Failure;
            BrainParameter parameter = m.RuntimeParameter;
            m.Perception.Sense(parameter != null ? parameter.WIData : null, m.Body.position, context.Time);
            return NodeStatus.Running;
        }
    }

    [Serializable, AINode("Character/Perception/See Target", Description = "True while a target is seen or was seen within Memory Seconds. Writes the target Transform and its last seen position. Use Abort Type Both on a chase branch.")]
    public sealed class SeeTargetNode : ConditionNode
    {
        [SerializeField, Min(0f)] float memorySeconds = 6f;
        [SerializeField] BlackboardRef<Transform> targetKey = new BlackboardRef<Transform>();
        [SerializeField] BlackboardRef<Vector3> positionKey = new BlackboardRef<Vector3>();
        [NonSerialized] AIBrainModule module;

        public float MemorySeconds
        {
            get => memorySeconds;
            set => memorySeconds = value;
        }

        public BlackboardRef<Transform> TargetKey
        {
            get => targetKey;
            set => targetKey = value;
        }

        public BlackboardRef<Vector3> PositionKey
        {
            get => positionKey;
            set => positionKey = value;
        }

        public override bool Evaluate(AIContext context)
        {
            AIBrainModule m = CharacterAI.Resolve(context, this, ref module);
            if (m == null) return false;
            CharacterPerception perception = m.Perception;
            if (!perception.RemembersSeen(context.Time, memorySeconds)) return false;
            if (targetKey != null && targetKey.IsAssigned)
                context.Blackboard.Set(targetKey, perception.SeenCollider != null ? perception.SeenCollider.transform : null);
            if (positionKey != null && positionKey.IsAssigned) context.Blackboard.Set(positionKey, perception.SeenPosition);
            return true;
        }
    }

    [Serializable, AINode("Character/Perception/Hear Sound", Description = "True while a sound is heard or was heard within Memory Seconds. Writes the sound position.")]
    public sealed class HearSoundNode : ConditionNode
    {
        [SerializeField, Min(0f)] float memorySeconds = 6f;
        [SerializeField] BlackboardRef<Vector3> positionKey = new BlackboardRef<Vector3>();
        [NonSerialized] AIBrainModule module;

        public float MemorySeconds
        {
            get => memorySeconds;
            set => memorySeconds = value;
        }

        public BlackboardRef<Vector3> PositionKey
        {
            get => positionKey;
            set => positionKey = value;
        }

        public override bool Evaluate(AIContext context)
        {
            AIBrainModule m = CharacterAI.Resolve(context, this, ref module);
            if (m == null) return false;
            CharacterPerception perception = m.Perception;
            if (!perception.RemembersSound(context.Time, memorySeconds)) return false;
            if (positionKey != null && positionKey.IsAssigned) context.Blackboard.Set(positionKey, perception.SoundPosition);
            return true;
        }
    }

    [Serializable, AINode("Character/Perception/Track Target", Description = "Writes the seen target's live position into a key every tick. Running while a target exists, Failure when there is none.")]
    public sealed class TrackTargetNode : ActionNode
    {
        [SerializeField] BlackboardRef<Vector3> positionKey = new BlackboardRef<Vector3>();
        [NonSerialized] AIBrainModule module;

        public BlackboardRef<Vector3> PositionKey
        {
            get => positionKey;
            set => positionKey = value;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            AIBrainModule m = CharacterAI.Resolve(context, this, ref module);
            if (m == null) return NodeStatus.Failure;
            Collider target = m.Perception.SeenCollider;
            if (target == null) return NodeStatus.Failure;
            Vector3 position = target.transform.position;
            m.Perception.SeenPosition = position;
            if (positionKey != null && positionKey.IsAssigned) context.Blackboard.Set(positionKey, position);
            return NodeStatus.Running;
        }
    }

    [Serializable, AINode("Character/Perception/Lose Target", Description = "Forgets the seen target (and the heard sound when Forget Sound is on), clears the given keys, and succeeds.")]
    public sealed class LoseTargetNode : ActionNode
    {
        [SerializeField] bool forgetSound;
        [SerializeField] BlackboardRef<Transform> targetKey = new BlackboardRef<Transform>();
        [SerializeField] BlackboardRef<Vector3> positionKey = new BlackboardRef<Vector3>();
        [NonSerialized] AIBrainModule module;

        public bool ForgetSound
        {
            get => forgetSound;
            set => forgetSound = value;
        }

        public BlackboardRef<Transform> TargetKey
        {
            get => targetKey;
            set => targetKey = value;
        }

        public BlackboardRef<Vector3> PositionKey
        {
            get => positionKey;
            set => positionKey = value;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            AIBrainModule m = CharacterAI.Resolve(context, this, ref module);
            if (m == null) return NodeStatus.Failure;
            m.Perception.ForgetSeen();
            if (forgetSound) m.Perception.ForgetSound();
            if (targetKey != null && targetKey.IsAssigned) context.Blackboard.Set(targetKey, (Transform)null);
            if (positionKey != null && positionKey.IsAssigned) context.Blackboard.Set(positionKey, Vector3.zero);
            return NodeStatus.Success;
        }
    }

    [Serializable, AINode("Character/Perception/Look Around", Description = "Stops moving and turns the body on the spot for Duration seconds, then succeeds.")]
    public sealed class LookAroundNode : ActionNode
    {
        [SerializeField, Min(0f)] float duration = 2f;
        [SerializeField, Tooltip("Degrees per second.")] float turnSpeed = 90f;
        [NonSerialized] AIBrainModule module;
        [NonSerialized] float endTime;
        [NonSerialized] float direction;

        public float Duration
        {
            get => duration;
            set => duration = value;
        }

        public float TurnSpeed
        {
            get => turnSpeed;
            set => turnSpeed = value;
        }

        protected override void OnStart(AIContext context)
        {
            endTime = context.Time + duration;
            direction = context.Random.Next(2) == 0 ? -1f : 1f;
            context.Mover?.Stop();
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            AIBrainModule m = CharacterAI.Resolve(context, this, ref module);
            if (m == null) return NodeStatus.Failure;
            m.Body.Rotate(0f, direction * turnSpeed * context.DeltaTime, 0f, Space.World);
            return context.Time >= endTime ? NodeStatus.Success : NodeStatus.Running;
        }
    }
}

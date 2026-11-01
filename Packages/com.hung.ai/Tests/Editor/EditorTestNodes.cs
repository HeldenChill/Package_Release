using System;
namespace Hung.AI.Editor.Tests
{
    /// <summary>Condition with a typed bool key field, for key-type validation and paste rebinding tests.</summary>
    [Serializable]
    public sealed class BoolKeyProbeNode : ConditionNode
    {
        public BlackboardRef<bool> Flag = new BlackboardRef<bool>();

        public override bool Evaluate(AIContext context) => true;
    }

    /// <summary>Action under a Perception menu folder, for perception category tests.</summary>
    [Serializable, AINode("Test/Perception/Probe", Description = "Perception-path probe for category tests.")]
    public sealed class PerceptionProbeNode : ActionNode
    {
        protected override NodeStatus OnUpdate(AIContext context) => NodeStatus.Success;
    }
}

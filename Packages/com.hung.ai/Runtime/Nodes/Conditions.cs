using System;
using UnityEngine;
namespace Hung.AI
{
    public enum CompareOp
    {
        Equal = 0,
        NotEqual = 1,
        Less = 2,
        LessOrEqual = 3,
        Greater = 4,
        GreaterOrEqual = 5
    }

    [Serializable, AINode("Conditions/Is Set", Description = "True when the key holds a value: bool true, number not zero, string not empty, Vector3 not zero, object not null.")]
    public sealed class IsSetNode : ConditionNode
    {
        [SerializeField] BlackboardKeyRef key = new BlackboardKeyRef();

        public BlackboardKeyRef Key
        {
            get => key;
            set => key = value;
        }

        public override bool Evaluate(AIContext context) => context.Blackboard.IsSet(key);
    }

    [Serializable, AINode("Conditions/Compare Blackboard", Description = "Compares a key with a constant. Numbers (Int, Float, Enum) support every operator; Bool and String support Equal and NotEqual.")]
    public sealed class CompareBlackboardNode : ConditionNode
    {
        [SerializeField] BlackboardKeyRef key = new BlackboardKeyRef();
        [SerializeField] CompareOp op;
        [SerializeField] float number;
        [SerializeField] string text;
        [SerializeField] bool flag;

        public BlackboardKeyRef Key
        {
            get => key;
            set => key = value;
        }

        public CompareOp Op
        {
            get => op;
            set => op = value;
        }

        public float Number
        {
            get => number;
            set => number = value;
        }

        public string Text
        {
            get => text;
            set => text = value;
        }

        public bool Flag
        {
            get => flag;
            set => flag = value;
        }

        public override bool Evaluate(AIContext context)
        {
            Blackboard bb = context.Blackboard;
            int i = bb.IndexOf(key);
            if (i < 0) return false;
            switch (bb.GetValueType(i))
            {
                case BlackboardValueType.Int:
                case BlackboardValueType.Enum: return CompareNumber(bb.Get<int>(i), number);
                case BlackboardValueType.Float: return CompareNumber(bb.Get<float>(i), number);
                case BlackboardValueType.Bool: return CompareEquality(bb.Get<bool>(i) == flag, context);
                case BlackboardValueType.String: return CompareEquality(string.Equals(bb.Get<string>(i), text, StringComparison.Ordinal), context);
                default:
                    context.LogOnce(Id + "/type", $"[Hung.AI] '{Title}' cannot compare a {bb.GetValueType(i)} key. Use Is Set instead.");
                    return false;
            }
        }

        bool CompareNumber(float value, float target)
        {
            switch (op)
            {
                case CompareOp.Equal: return Mathf.Approximately(value, target);
                case CompareOp.NotEqual: return !Mathf.Approximately(value, target);
                case CompareOp.Less: return value < target;
                case CompareOp.LessOrEqual: return value <= target;
                case CompareOp.Greater: return value > target;
                case CompareOp.GreaterOrEqual: return value >= target;
                default: return false;
            }
        }

        bool CompareEquality(bool equal, AIContext context)
        {
            if (op == CompareOp.Equal) return equal;
            if (op == CompareOp.NotEqual) return !equal;
            context.LogOnce(Id + "/op", $"[Hung.AI] '{Title}' uses {op} on a non-number key. Only Equal and NotEqual are supported.");
            return false;
        }
    }

    [Serializable, AINode("Conditions/Random Chance", Description = "True with the given probability (0 to 1).")]
    public sealed class RandomChanceNode : ConditionNode
    {
        [SerializeField, Range(0f, 1f)] float probability = 0.5f;

        public float Probability
        {
            get => probability;
            set => probability = value;
        }

        public override bool Evaluate(AIContext context) => context.Random.NextDouble() < probability;
    }
}

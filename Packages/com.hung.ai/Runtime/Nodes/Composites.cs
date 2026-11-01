using System;
namespace Hung.AI
{
    [Serializable, AINode("Composites/Sequence", Description = "Runs children left to right. Fails on the first failure, succeeds when all succeed.")]
    public sealed class SequenceNode : CompositeNode
    {
        [NonSerialized] int current;

        protected override void OnStart(AIContext context)
        {
            current = 0;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            for (int i = 0; i < current; i++)
            {
                if (children[i] is ConditionNode condition && (condition.AbortType & AbortType.Self) != 0 && !condition.Evaluate(context))
                {
                    children[current]?.Abort(context);
                    return NodeStatus.Failure;
                }
            }
            while (current < children.Count)
            {
                NodeStatus status = TickChild(current, context);
                if (status != NodeStatus.Success) return status;
                current++;
            }
            return NodeStatus.Success;
        }
    }

    [Serializable, AINode("Composites/Selector", Description = "Tries children left to right. Succeeds on the first success, fails when all fail. Higher children flagged LowerPriority can interrupt lower ones.")]
    public sealed class SelectorNode : CompositeNode
    {
        [NonSerialized] int current;

        protected override void OnStart(AIContext context)
        {
            current = 0;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            for (int i = 0; i < current; i++)
            {
                if (AIGuards.LowerPriorityGuardPasses(children[i], context))
                {
                    children[current]?.Abort(context);
                    current = i;
                    break;
                }
            }
            while (current < children.Count)
            {
                NodeStatus status = TickChild(current, context);
                if (status != NodeStatus.Failure) return status;
                current++;
            }
            return NodeStatus.Failure;
        }
    }

    public enum ParallelPolicy
    {
        RequireOne = 0,
        RequireAll = 1
    }

    [Serializable, AINode("Composites/Parallel", Description = "Ticks every child each tick. Finishes by its success and failure policies (failure wins a tie) and aborts children still running.")]
    public sealed class ParallelNode : CompositeNode
    {
        [UnityEngine.SerializeField] ParallelPolicy successPolicy = ParallelPolicy.RequireOne;
        [UnityEngine.SerializeField] ParallelPolicy failurePolicy = ParallelPolicy.RequireOne;
        [NonSerialized] NodeStatus[] results;

        public ParallelPolicy SuccessPolicy
        {
            get => successPolicy;
            set => successPolicy = value;
        }

        public ParallelPolicy FailurePolicy
        {
            get => failurePolicy;
            set => failurePolicy = value;
        }

        public override void Bind(AIGraphAsset graph)
        {
            base.Bind(graph);
            results = new NodeStatus[children.Count];
        }

        protected override void OnStart(AIContext context)
        {
            for (int i = 0; i < results.Length; i++) results[i] = NodeStatus.None;
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            int count = children.Count;
            if (count == 0) return NodeStatus.Success;
            int successes = 0;
            int failures = 0;
            for (int i = 0; i < count; i++)
            {
                if (results[i] == NodeStatus.None || results[i] == NodeStatus.Running) results[i] = TickChild(i, context);
                if (results[i] == NodeStatus.Success) successes++;
                else if (results[i] == NodeStatus.Failure) failures++;
            }
            bool failed = failurePolicy == ParallelPolicy.RequireOne ? failures > 0 : failures == count;
            bool succeeded = successPolicy == ParallelPolicy.RequireOne ? successes > 0 : successes == count;
            if (!failed && !succeeded && successes + failures < count) return NodeStatus.Running;
            for (int i = 0; i < count; i++) children[i]?.Abort(context);
            return failed || !succeeded ? NodeStatus.Failure : NodeStatus.Success;
        }
    }

    [Serializable, AINode("Composites/Random Selector", Description = "Tries children in a random order until one succeeds.")]
    public sealed class RandomSelectorNode : CompositeNode
    {
        [NonSerialized] int[] order;
        [NonSerialized] int current;

        public override void Bind(AIGraphAsset graph)
        {
            base.Bind(graph);
            order = new int[children.Count];
        }

        protected override void OnStart(AIContext context)
        {
            current = 0;
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = context.Random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        protected override NodeStatus OnUpdate(AIContext context)
        {
            while (current < order.Length)
            {
                NodeStatus status = TickChild(order[current], context);
                if (status != NodeStatus.Failure) return status;
                current++;
            }
            return NodeStatus.Failure;
        }
    }
}

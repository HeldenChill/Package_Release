using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>Node with ordered children. Child order is execution order.</summary>
    [Serializable]
    public abstract class CompositeNode : AINode
    {
        [SerializeField] List<string> childIds = new List<string>();
        [NonSerialized] protected List<AINode> children = new List<AINode>();

        public IReadOnlyList<string> ChildIds => childIds ??= new List<string>();

        public IReadOnlyList<AINode> Children => children ??= new List<AINode>();

        public void AddChild(string childId) => (childIds ??= new List<string>()).Add(childId);

        public void InsertChild(int index, string childId) => (childIds ??= new List<string>()).Insert(index, childId);

        public bool RemoveChild(string childId) => childIds != null && childIds.Remove(childId);

        public override void Bind(AIGraphAsset graph)
        {
            childIds ??= new List<string>();
            children = new List<AINode>(childIds.Count);
            for (int i = 0; i < childIds.Count; i++) children.Add(graph.GetNode(childIds[i]));
        }

        protected override void OnAbort(AIContext context)
        {
            for (int i = 0; i < children.Count; i++) children[i]?.Abort(context);
        }

        protected NodeStatus TickChild(int index, AIContext context)
        {
            AINode child = children[index];
            if (child == null)
            {
                context.LogOnce(Id + "/child/" + index, $"[Hung.AI] '{Title}' child {index} (id '{childIds[index]}') is missing. It is treated as Failure.");
                return NodeStatus.Failure;
            }
            return child.Tick(context);
        }
    }

    /// <summary>Node with exactly one child that changes its result or timing.</summary>
    [Serializable]
    public abstract class DecoratorNode : AINode
    {
        [SerializeField] string childId;
        [NonSerialized] protected AINode child;

        public string ChildId
        {
            get => childId;
            set => childId = value;
        }

        public override void Bind(AIGraphAsset graph)
        {
            child = string.IsNullOrEmpty(childId) ? null : graph.GetNode(childId);
        }

        protected override void OnAbort(AIContext context)
        {
            child?.Abort(context);
        }

        protected NodeStatus TickChild(AIContext context)
        {
            if (child == null)
            {
                context.LogOnce(Id + "/child", $"[Hung.AI] '{Title}' has no child (id '{childId}'). It is treated as Failure.");
                return NodeStatus.Failure;
            }
            return child.Tick(context);
        }
    }

    /// <summary>Leaf that does work and may run over several ticks.</summary>
    [Serializable]
    public abstract class ActionNode : AINode { }

    /// <summary>Leaf that answers true or false in one tick. AbortType lets it interrupt running branches.</summary>
    [Serializable]
    public abstract class ConditionNode : AINode
    {
        [SerializeField] AbortType abortType;

        public AbortType AbortType
        {
            get => abortType;
            set => abortType = value;
        }

        public abstract bool Evaluate(AIContext context);

        protected sealed override NodeStatus OnUpdate(AIContext context) => Evaluate(context) ? NodeStatus.Success : NodeStatus.Failure;
    }
}

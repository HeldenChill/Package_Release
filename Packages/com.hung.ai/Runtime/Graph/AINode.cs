using System;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>
    /// Base of every node. Lifecycle: OnStart once on entry, OnUpdate every tick while Running,
    /// OnEnd once when it stops (Success, Failure or Aborted). When aborted, OnAbort runs before OnEnd.
    /// Renaming or moving a node class requires [UnityEngine.Scripting.APIUpdating.MovedFrom], because assets store full type names.
    /// </summary>
    [Serializable]
    public abstract class AINode
    {
        [SerializeField] string id = Guid.NewGuid().ToString("N");
        [SerializeField] string title;
        [SerializeField] Vector2 editorPosition;
        [NonSerialized] bool isRunning;
        [NonSerialized] NodeStatus lastStatus;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string Title
        {
            get => string.IsNullOrEmpty(title) ? GetType().Name : title;
            set => title = value;
        }

        public Vector2 EditorPosition
        {
            get => editorPosition;
            set => editorPosition = value;
        }

        public bool IsRunning => isRunning;

        public NodeStatus LastStatus => lastStatus;

        public NodeStatus Tick(AIContext context)
        {
            if (!isRunning)
            {
                isRunning = true;
                OnStart(context);
            }
            NodeStatus status = OnUpdate(context);
            if (status == NodeStatus.None || status == NodeStatus.Aborted) status = NodeStatus.Failure;
            lastStatus = status;
            if (status != NodeStatus.Running)
            {
                isRunning = false;
                OnEnd(context, status);
            }
            context.NotifyTicked(this, status);
            return status;
        }

        public void Abort(AIContext context)
        {
            if (!isRunning) return;
            OnAbort(context);
            isRunning = false;
            lastStatus = NodeStatus.Aborted;
            OnEnd(context, NodeStatus.Aborted);
            context.NotifyTicked(this, NodeStatus.Aborted);
        }

        /// <summary>Called once on the runtime copy before the first tick. Resolve child ids here.</summary>
        public virtual void Bind(AIGraphAsset graph) { }

        protected virtual void OnStart(AIContext context) { }

        protected abstract NodeStatus OnUpdate(AIContext context);

        protected virtual void OnEnd(AIContext context, NodeStatus status) { }

        protected virtual void OnAbort(AIContext context) { }
    }
}

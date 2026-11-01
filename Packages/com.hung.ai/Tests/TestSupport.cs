using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI.Tests
{
    /// <summary>Counts log messages containing a fragment while alive.</summary>
    internal sealed class LogCounter : IDisposable
    {
        readonly string fragment;
        public int Count { get; private set; }

        public LogCounter(string fragment)
        {
            this.fragment = fragment;
            Application.logMessageReceived += OnLog;
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            if (message.Contains(fragment)) Count++;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
        }
    }

    /// <summary>Leaf that returns Result and records lifecycle calls into a shared journal.</summary>
    [Serializable]
    public sealed class ScriptedAction : ActionNode
    {
        public static readonly List<string> Journal = new List<string>();
        public NodeStatus Result = NodeStatus.Running;

        protected override void OnStart(AIContext context) => Journal.Add(Id + ":start");

        protected override NodeStatus OnUpdate(AIContext context)
        {
            Journal.Add(Id + ":update");
            return Result;
        }

        protected override void OnEnd(AIContext context, NodeStatus status) => Journal.Add(Id + ":end:" + status);

        protected override void OnAbort(AIContext context) => Journal.Add(Id + ":abort");

        public static int CountOf(string entry)
        {
            int n = 0;
            for (int i = 0; i < Journal.Count; i++) if (Journal[i] == entry) n++;
            return n;
        }
    }

    /// <summary>Condition that reads a bool blackboard key.</summary>
    [Serializable]
    public sealed class FlagCondition : ConditionNode
    {
        public BlackboardRef<bool> Flag = new BlackboardRef<bool>();

        public override bool Evaluate(AIContext context) => context.Blackboard.Get<bool>(Flag);
    }

    /// <summary>Builds a graph in code, makes the runtime copy, and owns cleanup.</summary>
    internal sealed class GraphFixture : IDisposable
    {
        public readonly AIGraphAsset Asset = ScriptableObject.CreateInstance<AIGraphAsset>();
        AIGraphAsset runtime;
        public AIGraphRunner Runner { get; private set; }
        public AIGraphAsset Runtime => runtime;
        public AIContext Context => Runner.Context;
        public Blackboard Blackboard => Runner.Context.Blackboard;

        public GraphFixture()
        {
            ScriptedAction.Journal.Clear();
        }

        public T Add<T>(string id, T node) where T : AINode
        {
            node.Id = id;
            Asset.AddNode(node);
            return node;
        }

        public T Composite<T>(string id, T node, params string[] childIds) where T : CompositeNode
        {
            foreach (string c in childIds) node.AddChild(c);
            return Add(id, node);
        }

        public SequenceNode Sequence(string id, params string[] childIds) => Composite(id, new SequenceNode(), childIds);

        public SelectorNode Selector(string id, params string[] childIds) => Composite(id, new SelectorNode(), childIds);

        public ScriptedAction Action(string id, NodeStatus result = NodeStatus.Running) => Add(id, new ScriptedAction { Result = result });

        public FlagCondition Flag(string id, BlackboardKey key, AbortType abort = AbortType.None)
        {
            var node = Add(id, new FlagCondition());
            node.Flag = new BlackboardRef<bool>(key.Id);
            node.AbortType = abort;
            return node;
        }

        public BlackboardKey Key(string name, BlackboardValueType type) => Asset.Blackboard.Add(name, type);

        public void Start(string rootId, IAIMover mover = null, int seed = 1)
        {
            Asset.RootNodeId = rootId;
            runtime = Asset.CreateRuntimeCopy();
            Runner = new AIGraphRunner(runtime, new AIContext(new Blackboard(runtime.Blackboard), mover, null, seed));
        }

        public T Node<T>(string id) where T : AINode => (T)runtime.GetNode(id);

        public void SetFlag(BlackboardKey key, bool value) => Blackboard.Set(new BlackboardRef<bool>(key.Id), value);

        public NodeStatus Tick(float deltaTime = 0.1f) => Runner.Tick(deltaTime);

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Asset);
            if (runtime != null) UnityEngine.Object.DestroyImmediate(runtime);
        }
    }

    /// <summary>Scriptable IAIMover for tests. Allocation-free.</summary>
    internal sealed class FakeMover : IAIMover
    {
        public Vector3 Destination;
        public int SetCalls;
        public int StopCalls;
        public bool Accept = true;
        public bool Arrived;
        public float Remaining = 10f;

        public bool SetDestination(Vector3 destination)
        {
            SetCalls++;
            Destination = destination;
            return Accept;
        }

        public void Stop() => StopCalls++;

        public bool HasArrived => Arrived;

        public float RemainingDistance => Remaining;
    }
}

using System;
using System.Collections.Generic;
using Hung.AI;
using UnityEngine;
using BrainData = Gameplay.Character.BrainSystem.BrainData;
using BrainParameter = Gameplay.Character.BrainSystem.BrainParameter;
using WorldInterfaceData = Gameplay.Character.WorldInterface.WorldInterfaceData;
namespace Gameplay.Character.AIBridge.Tests
{
    /// <summary>Counts log messages containing a fragment while alive.</summary>
    internal sealed class LogCounter : IDisposable
    {
        readonly string fragment;

        public LogCounter(string fragment)
        {
            this.fragment = fragment;
            Application.logMessageReceived += OnLog;
        }

        public int Count { get; private set; }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            if (message.Contains(fragment)) Count++;
        }
    }

    /// <summary>A GameObject with AIAgent + AIBrainModule, a graph built in code, and fake sensor lists. Owns cleanup.</summary>
    internal sealed class BrainHarness : IDisposable
    {
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        public readonly AIGraphAsset Graph;
        public readonly GameObject Owner;
        public readonly AIBrainModule Module;
        public readonly AIAgent Agent;
        public readonly BrainData Data = new BrainData();
        public readonly BrainParameter Parameter = new BrainParameter();
        public readonly WorldInterfaceData World = new WorldInterfaceData { SeenColliders = new List<Collider>(), SoundColliders = new List<Collider>() };

        public BrainHarness()
        {
            Graph = ScriptableObject.CreateInstance<AIGraphAsset>();
            owned.Add(Graph);
            Owner = new GameObject("BrainHarness");
            owned.Add(Owner);
            Module = Owner.AddComponent<AIBrainModule>();
            Agent = Owner.GetComponent<AIAgent>();
            Parameter.WIData = World;
        }

        public BlackboardKey Key(string name, BlackboardValueType type) => Graph.Blackboard.Add(name, type);

        public T Add<T>(string id, T node) where T : AINode
        {
            node.Id = id;
            Graph.AddNode(node);
            return node;
        }

        public void Start(string rootId)
        {
            Graph.RootNodeId = rootId;
            Agent.SetGraph(Graph);
            Module.Initialize(Data, Parameter);
        }

        public NodeStatus Tick(float deltaTime = 0.1f) => Module.TickBrain(deltaTime);

        public T Get<T>(BlackboardKey key) => Agent.Blackboard.Get<T>(new BlackboardRef<T>(key.Id));

        public GameObject Target(string name, Vector3 position, bool seen, bool heard)
        {
            var target = new GameObject(name);
            owned.Add(target);
            target.transform.position = position;
            var collider = target.AddComponent<BoxCollider>();
            if (seen) World.SeenColliders.Add(collider);
            if (heard) World.SoundColliders.Add(collider);
            return target;
        }

        public void Dispose()
        {
            if (Agent != null) Agent.ReleaseRuntime();
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
        }
    }
}

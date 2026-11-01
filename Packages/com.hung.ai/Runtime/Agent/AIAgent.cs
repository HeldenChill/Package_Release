using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>
    /// Runs an AIGraphAsset on this GameObject. Ticks itself in Update when AutoTick is on;
    /// a host system can turn AutoTick off and call Tick(deltaTime) itself.
    /// </summary>
    [DisallowMultipleComponent]
    public class AIAgent : MonoBehaviour
    {
        [SerializeField] AIGraphAsset graph;
        [SerializeField] bool autoTick = true;
        [SerializeField, Min(0f), Tooltip("Seconds between ticks. 0 ticks every frame.")] float tickInterval;
        [SerializeField, Tooltip("0 uses a random seed.")] int randomSeed;
        [SerializeField] List<BlackboardKeyOverride> overrides = new List<BlackboardKeyOverride>();
        AIGraphAsset runtimeGraph;
        AIGraphRunner runner;
        IAIMover pendingMover;
        float accumulated;
        bool paused;

        public AIGraphAsset Graph => graph;

        public AIGraphAsset RuntimeGraph => runtimeGraph;

        public Blackboard Blackboard => runner?.Context.Blackboard;

        public AIContext Context => runner?.Context;

        public List<BlackboardKeyOverride> Overrides => overrides ??= new List<BlackboardKeyOverride>();

        public bool IsPaused => paused;

        public bool IsInitialized => runner != null;

        public bool AutoTick
        {
            get => autoTick;
            set => autoTick = value;
        }

        public float TickInterval
        {
            get => tickInterval;
            set => tickInterval = Mathf.Max(0f, value);
        }

        public IAIMover Mover
        {
            get => runner != null ? runner.Context.Mover : pendingMover;
            set
            {
                pendingMover = value;
                if (runner != null) runner.Context.Mover = value;
            }
        }

        void Awake()
        {
            Initialize();
        }

        void Update()
        {
            if (!autoTick) return;
            if (tickInterval <= 0f)
            {
                Tick(Time.deltaTime);
                return;
            }
            accumulated += Time.deltaTime;
            if (accumulated < tickInterval) return;
            Tick(accumulated);
            accumulated = 0f;
        }

        void OnDisable()
        {
            AbortActivePath();
        }

        void OnDestroy()
        {
            ReleaseRuntime();
        }

        /// <summary>Replaces the graph. Releases any running copy; call Initialize afterwards.</summary>
        public void SetGraph(AIGraphAsset newGraph)
        {
            ReleaseRuntime();
            graph = newGraph;
        }

        /// <summary>Creates the runtime copy, blackboard and runner. Safe to call more than once.</summary>
        public bool Initialize()
        {
            if (runner != null) return true;
            if (graph == null)
            {
                Debug.LogWarning($"[Hung.AI] AIAgent '{name}' has no graph.", this);
                return false;
            }
            runtimeGraph = graph.CreateRuntimeCopy();
            var blackboard = new Blackboard(runtimeGraph.Blackboard, Overrides);
            IAIMover mover = pendingMover;
            if (mover == null && TryGetComponent(out IAIMover found)) mover = found;
            int seed = randomSeed != 0 ? randomSeed : Environment.TickCount ^ GetInstanceID();
            if (seed == 0) seed = 1;
            runner = new AIGraphRunner(runtimeGraph, new AIContext(blackboard, mover, gameObject, seed));
            return true;
        }

        public NodeStatus Tick(float deltaTime)
        {
            if (paused || runner == null) return NodeStatus.None;
            return runner.Tick(deltaTime);
        }

        /// <summary>Ends the active path cleanly and stops ticking until Resume. Use it when a scripted sequence takes the body.</summary>
        public void Pause()
        {
            if (paused) return;
            AbortActivePath();
            Mover?.Stop();
            paused = true;
        }

        /// <summary>Resumes ticking; the tree restarts from its root.</summary>
        public void Resume()
        {
            paused = false;
        }

        /// <summary>Aborts the running branch (OnAbort then OnEnd run). The next tick starts from the root.</summary>
        public void AbortActivePath()
        {
            runner?.Abort();
        }

        /// <summary>Aborts and destroys the runtime copy.</summary>
        public void ReleaseRuntime()
        {
            AbortActivePath();
            runner = null;
            if (runtimeGraph == null) return;
            if (Application.isPlaying) Destroy(runtimeGraph);
            else DestroyImmediate(runtimeGraph);
            runtimeGraph = null;
        }
    }
}

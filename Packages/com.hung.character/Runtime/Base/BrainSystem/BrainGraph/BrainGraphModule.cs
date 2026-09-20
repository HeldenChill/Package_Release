using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Character.BrainSystem
{
    /// <summary>
    /// BrainModule implementation that runs a local Character BrainGraph without Unity Behavior Graph runtime dependency.
    /// The graph reads BrainParameter and writes BrainData only; Logic/Physic still consume BrainData through CharacterBrainSystem.
    /// </summary>
    public class BrainGraphModule : BrainModule
    {
        [Header("Brain Graph")]
        [SerializeField] private BrainGraphAsset graph;
        [SerializeField] private bool runOnUpdate = true;
        [SerializeField] private bool debugRuntime = true;

        [Header("Optional Movement")]
        [SerializeField] private NavMeshAgent navMeshAgent;

        private BrainGraphRunner runner;
        private BrainGraphStatus lastStatus = BrainGraphStatus.None;

        public BrainGraphAsset Graph => graph;
        public BrainGraphRunner Runner => runner;
        public BrainGraphRuntimeDebugState DebugState => runner != null ? runner.DebugState : null;
        public bool DebugRuntime => debugRuntime;
        public BrainGraphStatus LastStatus => lastStatus;
        public BrainData RuntimeData => Data;
        public BrainParameter RuntimeParameter => Parameter;
        public NavMeshAgent NavMeshAgent => navMeshAgent;

        private void Reset()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            BrainGraphRuntimeRegistry.Register(this);
        }

        private void OnDisable()
        {
            BrainGraphRuntimeRegistry.Unregister(this);
        }

        public override void Initialize(BrainData data, BrainParameter parameter)
        {
            base.Initialize(data, parameter);
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
            runner = graph != null ? new BrainGraphRunner(graph, this, data, parameter, navMeshAgent) : null;
            ConfigureNavMeshAgent();
        }

        public void SetGraph(BrainGraphAsset newGraph)
        {
            graph = newGraph;
            if (Data != null && Parameter != null)
            {
                runner = graph != null ? new BrainGraphRunner(graph, this, Data, Parameter, navMeshAgent) : null;
            }
        }

        public override void StartNavigation()
        {
            ConfigureNavMeshAgent();
        }

        public override void StopNavigation()
        {
            if (Data != null)
            {
                Data.MoveDirection = Vector3.zero;
                Data.HasMoveTarget = false;
            }
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
            }
        }

        public override void UpdateData()
        {
            if (!runOnUpdate || graph == null || runner == null) return;
            lastStatus = runner.Tick();
            if (debugRuntime && runner.DebugState != null)
            {
                // Runtime state is intentionally stored, not logged every frame.
                // The custom BrainGraph editor reads this through BrainGraphRuntimeRegistry.
            }
        }

        public override void FixedUpdateData()
        {
        }

        private void ConfigureNavMeshAgent()
        {
            if (navMeshAgent == null) return;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
        }
    }
}

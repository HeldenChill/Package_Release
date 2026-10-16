using Hung.AI;
using UnityEngine;
using UnityEngine.AI;
using BrainData = Gameplay.Character.BrainSystem.BrainData;
using BrainModule = Gameplay.Character.BrainSystem.BrainModule;
using BrainParameter = Gameplay.Character.BrainSystem.BrainParameter;
namespace Gameplay.Character.AIBridge
{
    /// <summary>
    /// BrainModule that runs a Hung.AI graph. The AIAgent on the same GameObject holds the graph and per-agent overrides;
    /// this module turns the agent's auto tick off and ticks it from CharacterBrainSystem, so the brain runs in the
    /// character's update order. Movement goes through CharacterMover into BrainData.MoveDirection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AIAgent))]
    public class AIBrainModule : BrainModule
    {
        [SerializeField, Tooltip("Transform the mover turns and measures from. Empty uses this transform.")] Transform body;
        [SerializeField, Tooltip("Optional. When set, paths come from the NavMesh. The NavMeshAgent never moves or turns the body.")] NavMeshAgent navMeshAgent;
        [SerializeField, Min(0f), Tooltip("Flat distance at which a move target counts as reached.")] float arriveDistance = 0.2f;
        [SerializeField, Min(0f), Tooltip("Degrees per second the body turns toward its move direction.")] float turnSpeed = 360f;
        [SerializeField] BrainDataMap brainDataMap = new BrainDataMap();
        readonly CharacterPerception perception = new CharacterPerception();
        AIAgent agent;
        CharacterMover mover;

        public AIAgent Agent => agent;

        public CharacterMover Mover => mover;

        public CharacterPerception Perception => perception;

        public BrainDataMap BrainDataMap => brainDataMap ??= new BrainDataMap();

        public BrainData RuntimeData => Data;

        public BrainParameter RuntimeParameter => Parameter;

        public Transform Body => body != null ? body : transform;

        void Reset()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        public override void Initialize(BrainData data, BrainParameter parameter)
        {
            base.Initialize(data, parameter);
            agent = GetComponent<AIAgent>();
            agent.AutoTick = false;
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.updatePosition = false;
                navMeshAgent.updateRotation = false;
            }
            mover = new CharacterMover(data, Body, navMeshAgent) { ArriveDistance = arriveDistance, TurnSpeed = turnSpeed };
            agent.Mover = mover;
            agent.Initialize();
        }

        public override void UpdateData()
        {
            TickBrain(Time.deltaTime);
        }

        /// <summary>One brain step: BrainData to blackboard, tick the graph, blackboard to BrainData. None when the agent cannot run.</summary>
        public NodeStatus TickBrain(float deltaTime)
        {
            if (agent == null || !agent.IsInitialized || agent.IsPaused || Data == null) return NodeStatus.None;
            mover.BeginTick(deltaTime);
            BrainDataMap.Pull(Data, agent.Blackboard);
            NodeStatus status = agent.Tick(deltaTime);
            BrainDataMap.Push(agent.Blackboard, Data);
            return status;
        }

        public override void StartNavigation()
        {
            if (agent != null) agent.Resume();
        }

        public override void StopNavigation()
        {
            if (agent != null) agent.Pause();
            mover?.Stop();
        }

        public override void ResetModule()
        {
            if (agent != null) agent.AbortActivePath();
            mover?.Stop();
            perception.Clear();
        }
    }
}

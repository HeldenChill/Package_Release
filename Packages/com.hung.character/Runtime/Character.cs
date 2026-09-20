using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character
{
    using WorldInterface;
    using BrainSystem;
    using Logic;
    using Physic;
    using Hung.DesignPattern;

    public interface ICharacter
    {
        public Transform Tf { get; }
    }
    public abstract class CharacterBase<TDef, TStats> : GameUnit, ICharacter
        where TDef : CharacterDefinition
        where TStats : CharacterStats, new()
    {
        [SerializeField]
        protected TDef Definition;
        protected TStats RuntimeStats;
        [SerializeField]
        protected WorldInterfaceModule WorldInterfaceModule;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("NavigationModule")]
        protected BrainModule NavigationModule;
        [SerializeField]
        protected LogicModule LogicModule;
        [SerializeField]
        protected A3DPhysicModule PhysicModule;
        [SerializeField]
        protected CharacterVisualModule VisualModule;
        protected WorldInterfaceSystem WorldInterfaceSystem;
        protected CharacterBrainSystem NavigationSystem;
        protected LogicSystem LogicSystem;
        protected C3DPhysicSystem PhysicSystem;

        [HideInInspector]
        public PerceptionData PerceptionData;

        #region Factory virtuals
        protected virtual LogicEvent CreateLogicEvent() => new LogicEvent();
        protected virtual LogicParameter CreateLogicParameter() => new LogicParameter();
        protected virtual BrainData CreateNavigationData() => new BrainData();
        protected virtual BrainParameter CreateNavigationParameter() => new BrainParameter();
        protected virtual LogicSystem CreateLogicSystem(LogicModule module, PerceptionData perceptionData)
            => new LogicSystem(module, perceptionData, CreateLogicEvent(), CreateLogicParameter());
        protected virtual CharacterBrainSystem CreateNavigationSystem(BrainModule module, PerceptionData perceptionData)
            => new CharacterBrainSystem(module, perceptionData, CreateNavigationData(), CreateNavigationParameter());
        protected virtual C3DPhysicSystem CreatePhysicSystem(A3DPhysicModule module, PerceptionData perceptionData)
            => new C3DPhysicSystem(module, perceptionData);
        protected virtual WorldInterfaceSystem CreateWorldInterfaceSystem(WorldInterfaceModule module, PerceptionData perceptionData)
            => new WorldInterfaceSystem(module, perceptionData);
        #endregion

        protected bool initialized;

        /// <summary>
        /// Builds runtime stats and every character system. A spawner may call this
        /// before the object activates to inject a definition; otherwise Awake calls
        /// it with the serialized one. Runs at most once either way.
        /// </summary>
        public virtual void OnInit(TDef definition = null)
        {
            if (initialized) return;
            initialized = true;

            if (definition != null) Definition = definition;

            PerceptionData = new PerceptionData();
            RuntimeStats = (TStats)Definition.BuildRuntime();
            PerceptionData.Initialize(Tf, SkinTf)
            .SetCharacter(this);
            WorldInterfaceSystem = CreateWorldInterfaceSystem(WorldInterfaceModule, PerceptionData);
            NavigationSystem = CreateNavigationSystem(NavigationModule, PerceptionData);
            LogicSystem = CreateLogicSystem(LogicModule, PerceptionData);
            PhysicSystem = CreatePhysicSystem(PhysicModule, PerceptionData);
        }

        protected virtual void Awake()
        {
            OnInit();
        }

        protected virtual void OnEnable()
        {
            #region Update Data Event
            NavigationSystem.ReceiveInformation(WorldInterfaceSystem.Data);
            NavigationSystem.ReceiveInformation(RuntimeStats);
            LogicSystem.ReceiveInformation(WorldInterfaceSystem.Data);
            LogicSystem.ReceiveInformation(NavigationSystem.Data);
            LogicSystem.ReceiveInformation(PhysicSystem.Data);
            LogicSystem.ReceiveInformation(RuntimeStats);
            #endregion
            LogicSystem.Event._SetVelocity += PhysicModule.SetVelocity;
            LogicSystem.Event._AddForce += PhysicModule.AddForce;
            LogicSystem.Event._SetLocalVelocityXZ += PhysicModule.SetLocalVelocityXZ;
            LogicSystem.Event._SetAnimFloat += VisualModule.SetAnimFloat;
        }

        protected virtual void OnDisable()
        {
            LogicSystem.Event._SetVelocity -= PhysicModule.SetVelocity;
            LogicSystem.Event._AddForce -= PhysicModule.AddForce;
            LogicSystem.Event._SetLocalVelocityXZ -= PhysicModule.SetLocalVelocityXZ;
            LogicSystem.Event._SetAnimFloat -= VisualModule.SetAnimFloat;
        }


        protected virtual void Update()
        {
            NavigationSystem.Run();
            LogicSystem.Run();
            PhysicSystem.Run();
        }

        protected virtual void FixedUpdate()
        {
            WorldInterfaceSystem.FixedUpdateData();
            WorldInterfaceSystem.Run();
            // Fix J (camera jitter): mirror the sensor's ground state into PhysicData so the physics
            // module can clamp vertical velocity while grounded, without owning a second grounded
            // check of its own. Runs right after the sensors update, before anything reads it.
            // See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
            PhysicSystem.Data.IsGrounded = WorldInterfaceSystem.Data.IsGrounded;
            NavigationSystem.FixedUpdateData();
            // Fix C (camera jitter): apply velocity buffered from the render clock before the
            // state machine runs, so a state's own FixedUpdate write still wins over a stale one.
            // See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
            LogicSystem.Event.FlushPendingVelocity();
            LogicSystem.FixedUpdateData();
            PhysicSystem.FixedUpdateData();

        }


    }
}

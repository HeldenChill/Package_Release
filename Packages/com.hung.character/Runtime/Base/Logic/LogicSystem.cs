using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gameplay.Character.Logic
{
    using WorldInterface;
    using BrainSystem;
    using Physic;
    using Gameplay.Character;
    using System;
    using Gameplay.Character;

    /// <summary>
    /// Responsibility for updating interaction logic between DynamicObject and the game world
    /// </summary>
    public class LogicSystem : System<LogicModule, LogicData, LogicParameter>
    {
        //Initialize
        public LogicEvent Event;
        public LogicSystem(LogicModule module, PerceptionData characterData, LogicEvent eventOverride = null, LogicParameter parameterOverride = null)
        {
            data = new LogicData();
            Parameter = parameterOverride ?? new LogicParameter();
            Event = eventOverride ?? new LogicEvent();
            this.module = module;
            data.PerceptionData = characterData;
            Parameter.PerceptionData = characterData;
            module.Initialize(data, Parameter, Event);
        }

        #region ReceiveInformation Functions
        //Need to update this ReceiveInformation
        public void ReceiveInformation(PhysicData Data)
        {
            Parameter.PhysicData = Data;
        }
        public void ReceiveInformation(WorldInterfaceData worldInterface)
        {
            Parameter.WIData = worldInterface;
        }

        public void ReceiveInformation(BrainData navigation)
        {
            Parameter.NavData = navigation;
        }
        public void ReceiveInformation<T>(T stats) where T : CharacterStats
        {
            Parameter.SetStats(stats);
        }
        public void ReceiveInformation(Type type, string name)
        {
            Parameter.OnAnimationTriggerEvent?.Invoke(type, name);
        }

        public void ReceiveInformation(Type type,AnimationClip clip)
        {
            Parameter.OnReceiveAnimationClipData?.Invoke(type,clip);
        }
        #endregion
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gameplay.Character.BrainSystem
{
    using Gameplay.Character;
    using Gameplay.Character.WorldInterface;

    /// <summary>
    /// Responsibility for navigating the Dynamic Object (Player or Agent)
    /// </summary>
    public class CharacterBrainSystem : System<BrainModule, BrainData, BrainParameter>
    {
        #region System Components
        //NavigationDecision = Core
        public BrainModule Module { get => module; set => module = value; }
        #endregion
        #region Essential Functions
        protected CharacterBrainSystem() { }
        //Initialize
        public CharacterBrainSystem(BrainModule module, PerceptionData characterData, BrainData dataOverride = null, BrainParameter parameterOverride = null)
        {
            data = dataOverride ?? new BrainData();
            Parameter = parameterOverride ?? new BrainParameter();
            this.module = module;
            data.PerceptionData = characterData;
            Parameter.PerceptionData = characterData;
            base.module.Initialize(data, Parameter);
        }
        #endregion

        #region ReceiveInformation Functions
        public virtual void ReceiveInformation(WorldInterfaceData worldInterface)
        {
            Parameter.WIData = worldInterface;
        }

        public void ReceiveInformation<T>(T stats) where T : CharacterStats
        {
            Parameter.SetStats(stats);
        }
        #endregion
    }
}

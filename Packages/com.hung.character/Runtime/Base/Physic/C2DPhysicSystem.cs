using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Character;
namespace Gameplay.Character.Physic
{
    public class C2DPhysicSystem : System<A2DPhysicModule,PhysicData,PhysicParameter>
    {
        #region Attributes
        public A2DPhysicModule MovementModule { get => module; set => module = value; }
        #endregion
        public C2DPhysicSystem(A2DPhysicModule module, PerceptionData characterData)
        {
            data = new PhysicData();
            Parameter = new PhysicParameter();
            data.PerceptionData = characterData;
            Parameter.PerceptionData = characterData;
            this.module = module;
            module.Initialize(data, Parameter);
        }
    }
}
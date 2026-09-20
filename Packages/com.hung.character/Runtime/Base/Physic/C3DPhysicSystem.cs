using UnityEngine;
using Gameplay.Character.Physic;
using Gameplay.Character;
namespace Gameplay.Character.Physic
{
    public class C3DPhysicSystem : System<A3DPhysicModule, PhysicData, PhysicParameter>
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        #region Attributes
        public A3DPhysicModule MovementModule { get => module; set => module = value; }
        #endregion
        public C3DPhysicSystem(A3DPhysicModule module, PerceptionData characterData)
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
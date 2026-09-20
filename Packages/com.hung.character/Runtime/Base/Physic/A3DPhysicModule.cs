using UnityEngine;
using Gameplay.Character.Physic;
using Gameplay.Character;

namespace Gameplay.Character.Physic
{
    public abstract class A3DPhysicModule : Module<PhysicData, PhysicParameter>
    {
        [SerializeField]
        protected Rigidbody rb;
        [SerializeField]
        protected CapsuleCollider capsuleCollider;

        public override void Initialize(PhysicData Data, PhysicParameter Parameter)
        {
            this.Data = Data;
            this.Parameter = Parameter;
        }

        public abstract void SetVelocity(Vector3 velocity);
        public abstract void SetLocalVelocityXZ(Vector3 velocity);
        public abstract void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse);
        public abstract void SetColliderSize(Vector3 size, float time = -1);
        public abstract void SetColliderOffset(Vector3 offset, float time = -1);
        public abstract void LookTowardPosition(Vector3 position);
    }
}
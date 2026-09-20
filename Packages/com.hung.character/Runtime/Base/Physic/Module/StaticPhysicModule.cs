using UnityEngine;

namespace Gameplay.Character.Physic
{
    /// <summary>
    /// Null Object physics for non-moving negatives. Physics event consumers become no-ops.
    /// </summary>
    public class StaticPhysicModule : A3DPhysicModule
    {
        public override void UpdateData() { }
        public override void SetVelocity(Vector3 velocity) { }
        public override void SetLocalVelocityXZ(Vector3 velocity) { }
        public override void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse) { }
        public override void SetColliderSize(Vector3 size, float time = -1) { }
        public override void SetColliderOffset(Vector3 offset, float time = -1) { }
        public override void LookTowardPosition(Vector3 position) { }
    }
}

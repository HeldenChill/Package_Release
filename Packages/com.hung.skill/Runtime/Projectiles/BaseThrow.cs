using Hung.Base;
using Hung.DesignPattern;
using UnityEngine;

namespace Hung.Skill
{
    public class BaseThrow : BaseProjectile
    {
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isProjectiling)
            {
                SkinTf.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
        }

        protected void OnTriggerEnter(Collider collision)
        {
            int groundMask = LayerMask.NameToLayer(SKILL_CONSTANTS.GROUND_LAYER);

            if (collision.gameObject.layer == groundMask)
            {
                OnGroundCollide();
                this.Despawn();
            }
        }

        protected virtual void OnGroundCollide()
        {

        }
    }
}

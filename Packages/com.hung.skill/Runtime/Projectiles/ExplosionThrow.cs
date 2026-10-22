using Hung.Combat;
using Hung.DesignPattern;
using UnityEngine;

namespace Hung.Skill
{
    public class ExplosionThrow : BaseThrow
    {
        protected override void OnGroundCollide()
        {
            AOEDamage explosion = SimplePool.Spawn<AOEDamage>(PoolType.FIRE_BALL_EXPLOSION, Tf.position + Vector3.up * 0.5f, Quaternion.identity);
            explosion.Damage = Data.Damage;
        }
    }
}

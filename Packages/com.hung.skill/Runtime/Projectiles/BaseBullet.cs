using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Skill
{
    using Hung.Base;
    using Hung.Combat;
    using global::DesignPattern;
    using Hung.DesignPattern;

    public class BaseBullet : BaseProjectile
    {      
        [HideInInspector]
        public ObjectContainer Container;
        [HideInInspector]
        public SkillObjContainer PoolData;
        [HideInInspector]
        public int PoolID = 0;
        [HideInInspector]
        public int Hp = 1;
        [SerializeField]
        EffectVFXController effectVFX;
        public EffectVFXController EffectVFX => effectVFX;
      
        protected virtual void OnTriggerEnter(Collider collision)
        {
            int characterMask = LayerMask.NameToLayer(SKILL_CONSTANTS.ENEMY_COLLIDER_LAYER);
            if (collision.gameObject.layer == characterMask)
            {
                IDamageable enemy = collision.gameObject.GetComponent<IDamageable>();
                if (enemy.Type == CHARACTER_TYPE.ENEMY)
                {
                    float value = enemy.TakeDamage(DamageHitBuilders.From(-Data.Damage, ELEMENTAL.NONE, source));
                    for(int i = 0; i < Data.Effects.Count; i++)
                    {
                        enemy.TakeDamage(Data.Effects[i], Data.EffectTimes[i], Tf.position, 1, source);
                    }
                    Hp--;
                    if (Hp <= 0)
                    {
                        OnDespawn();
                    }
                }
            }
            int groundMask = LayerMask.NameToLayer(SKILL_CONSTANTS.GROUND_LAYER);

            if (collision.gameObject.layer == groundMask)
            {
                OnDespawn();
            }
        }
        public override void OnDespawn()
        {
            if (Container != null)
            {
                gameObject.SetActive(false);
                PoolData.Tf.parent = Container.transform;
                Container.Push(PoolID, PoolData);
            }
            else
            {
                isProjectiling = false;
                this.Despawn();
            }
        }       
    }
}
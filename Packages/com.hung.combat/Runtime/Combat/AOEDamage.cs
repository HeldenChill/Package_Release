using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Hung.Combat
{
    using Hung.Base;
    public class AOEDamage : AOEDetection<IDamageable>
    {
        [Serializable]
        protected class EffectData
        {
            public EFFECT Type;
            public float Time;
            public float Damage;
            public EffectData(EFFECT type, float time, float damage = 0)
            {
                this.Type = type;
                this.Time = time;
                this.Damage = damage;
            }
        }
        [SerializeField]
        protected string ENEMY_LAYER = "EnemyCollider";

        #region Property
        [SerializeField]
        ELEMENTAL elemental = ELEMENTAL.NONE;
        [SerializeField]
        List<EffectData> effects;
        [HideInInspector]
        public float EffectHit = 1;
        #endregion

        protected virtual void Awake()
        {

            switch (space)
            {
                case SPACE.D2:
                    targetDetection = new Target2DDetection<IDamageable>(new string[] { ENEMY_LAYER }, null, 30);
                    break;
                case SPACE.D3:
                    targetDetection = new Target3DDetection<IDamageable>(new string[] { ENEMY_LAYER }, null, 30);
                    break;
            }
        }
        
        protected virtual void OnEnable()
        {
            if (autoTrigger)
            {
                Trigger();
            }
            else
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }        
        public override void Trigger()
        {
            base.Trigger();
            List<Cube> cubes = CUBE_UTILITIES.SphereToCube(explodeTF.position, AOERange);
            RegisterArea(AREA_TYPE.DANGER, cubes, 1f, timeExplode, this);
        }
        
        protected override void DealDamage(int damage, float aoeRange)
        {           
            aoeRangeTemp = aoeRange;
            targetDetection.CheckTargets(explodeTF.position, aoeRange, ref targets);
            base.DealDamage(damage, aoeRange);
            
            if (type == TYPE.INCREASE_SINGLE_EXPLODE) //NOTE: Remove all targets which be affected already and add new target that be affected
            {
                targets.RemoveAll(x => oldTargets.Exists(y => x == y));
                oldTargets.AddRange(targets);
            }


            for (int i = 0; i < targets.Count; i++)
            {
                if (damage > 0)
                    targets[i].TakeDamage(DamageHitBuilders.From(-damage, elemental, this));
                for (int j = 0; j < effects.Count; j++)
                {
                    InflictEffect(effects[j], targets[i]);
                }
                //targets[i].RunTakeDamageAnim();
                //targets[i].SpawnHitImpact(hitImpactType);
            }

        }

        public void AddEffect(EFFECT effectType, float time, float damage = 0)
        {
            EffectData effData = new EffectData(effectType, time, damage);
            effects.Add(effData);
        }

        public void RemoveAllEffect()
        {
            effects.Clear();
        }

        protected virtual void InflictEffect(EffectData effect, IDamageable enemy)
        {
            if (enemy == null) return;
            enemy?.TakeDamage(effect.Type, effect.Time, Tf.position, effect.Damage, this);
        }
    }
}
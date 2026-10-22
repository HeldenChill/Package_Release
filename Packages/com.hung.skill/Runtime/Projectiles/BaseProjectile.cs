using UnityEngine;

namespace Hung.Skill
{
    using System;
    using System.Collections.Generic;
    using Hung.Base;
    using Hung.DesignPattern;
    using Hung.Utilities.Timer;

    public class BaseProjectile : GameUnit
    {
        public Action<Vector3> _OnRuneExplosion;

        public class Propertys
        {
            public float Damage;
            public ELEMENTAL Elemental;
            public List<EFFECT> Effects = new List<EFFECT>();
            public List<float> EffectTimes = new List<float>();
        }
        [SerializeField]
        protected Rigidbody rb;
        [SerializeField]
        protected ICharacter source;

        protected bool isProjectiling = false;
        protected STimer timer;
        protected Propertys data = new Propertys();
        public Propertys Data => data;
        public virtual void Shot(Vector3 direction, float speed, ICharacter source = null)
        {
            this.source = source;
            Tf.rotation = Quaternion.LookRotation(direction);
            rb.linearVelocity = Tf.forward * speed;
            isProjectiling = true;
        }

        public override void OnDespawn()
        {
            isProjectiling = false;
            this.Despawn();
        }

        protected virtual void FixedUpdate()
        {
           
        }
        
    }
}

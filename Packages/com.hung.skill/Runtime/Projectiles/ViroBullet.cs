using Hung.Base;
using UnityEngine;
using Hung.Utilities.Timer;

namespace Hung.Skill
{
    public class ViroBullet : BaseBullet
    {
        [HideInInspector]
        public float returnTime = 2f;
        STimer returnTimer;
        bool isCanDespawn = false;

        protected virtual void Awake()
        {
            returnTimer = TimerManager.Ins.PopSTimer();
        }

        public override void Shot(Vector3 direction, float speed, ICharacter source = null)
        {
            base.Shot(direction, speed, source);
            isCanDespawn = false;
            returnTimer.Start(returnTime, () => FlyBack(speed, source));
        }

        protected void FlyBack(float speed, ICharacter source)
        {
            Vector3 backDirection = source.Tf.position - Tf.position;
            isCanDespawn = true;
            base.Shot(backDirection, speed, source);
        }

        protected override void OnTriggerEnter(Collider collision)
        {
            base.OnTriggerEnter(collision);
            int playerMask = LayerMask.NameToLayer(SKILL_CONSTANTS.PLAYER_COLLIDER_LAYER);
            if (collision.gameObject.layer == playerMask)
            {
                if (isCanDespawn)
                {
                    OnDespawn();                  
                }
            }
        }
    }
}

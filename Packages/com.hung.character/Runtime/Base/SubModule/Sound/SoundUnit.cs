using Hung.DesignPattern;
using UnityEngine;
using Hung.Utilities.Timer;

namespace Gameplay
{
    public class SoundUnit : GameUnit
    {
        [SerializeField]
        protected SphereCollider soundCollider;
        STimer stimer;
        
        public SoundUnit Init(float radius)
        {
            soundCollider.radius = radius;
            gameObject.SetActive(true);
            stimer = TimerManager.Ins.PopSTimer();
            return this;
        }
        public SoundUnit SetTime(float time = -1)
        {
            if(time > 0)
            {
                stimer.Start(time, OnDespawn);
            }
            return this;
        }
        public override void OnDespawn()
        {
            base.OnDespawn();
            TimerManager.Ins.PushSTimer(stimer);
        }
    }
}
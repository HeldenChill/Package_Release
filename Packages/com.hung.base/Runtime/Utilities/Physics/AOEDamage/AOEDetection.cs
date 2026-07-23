using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    using Hung.DesignPattern;
    using Hung.Utilities.Timer;
    public abstract class AOEDetection<T> : GameUnit
    {
        public static event Action<AREA_TYPE, List<Cube>, float, float, object> _OnRegisterArea;
        public static event Action<object> _OnRemoveArea;
        public event Action<List<T>> _OnTargetDetection;
        public enum TYPE
        {
            SINGLE_EXPLODE = 0,
            MULTIPLE_EXPLODE = 1,
            INCREASE_MULTIPLE_EXPLODE = 2,
            INCREASE_SINGLE_EXPLODE = 3,
        }
        public enum SPACE
        {
            D2 = 0,
            D3 = 1,
        }
        [SerializeField]
        protected TYPE type;
        [SerializeField] 
        protected SPACE space;
        [Tooltip("Total damage is caused by all explosions")]
        public float Damage;
        public float AOERange = 1.5f;
        [Tooltip("Time from Trigger AOEDamage to disable it")]
        [SerializeField]
        protected float existTime = 1.5f;
        [Tooltip("The number of explosions that deal damage")]
        [SerializeField]
        [Range(1, 20)]
        protected int numOfExplode;
        [Tooltip("Total time to explode all explosions")]
        [SerializeField]
        [Range(0.1f, 5f)]
        protected float timeExplode;
        [SerializeField]
        protected Transform explodeTF;
        [Tooltip("Contains particle system of AOEDamage. If you don't want to use auto trigger, you need to ref this to particle system")]
        [SerializeField]
        protected ParticleSystem[] particleSystems;

        [Tooltip("Auto Trigger AOEDamage and its VFX when OnEnable run")]
        [SerializeField]
        protected bool autoTrigger = true;
        [Tooltip("Auto despawn and push to pool")]
        [SerializeField]
        protected bool autoPushToPool = false;
        [HideInInspector]
        protected Action ReleaseAction;

        protected BaseTargetDetection<T> targetDetection;
        protected List<T> targets = new List<T>();
        public IReadOnlyList<T> Targets => targets.AsReadOnly();
        protected List<T> oldTargets;
        protected float aoeRangeTemp;
        protected virtual void OnDisable()
        {
            RemoveArea(this);
        }
        public virtual void Trigger()
        {
            TimerManager.Ins.WaitForTime(existTime, () =>
            {
                gameObject.SetActive(false);
                oldTargets?.Clear();
                if (autoPushToPool)
                {
                    ReleaseAction?.Invoke();
                }
            });

            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i].isStopped)
                    particleSystems[i].Play();
            }

            switch (type)
            {
                case TYPE.SINGLE_EXPLODE:
                    DealDamage((int)Damage, AOERange);
                    break;
                case TYPE.MULTIPLE_EXPLODE:
                    aoeRangeTemp = AOERange;
                    TimerManager.Ins.TriggerLoopAction(() =>
                    {
                        DealDamage((int)Damage / numOfExplode, AOERange);
                    }, timeExplode / numOfExplode, numOfExplode);
                    break;
                case TYPE.INCREASE_MULTIPLE_EXPLODE:
                    int i = 0;
                    TimerManager.Ins.TriggerLoopAction(() =>
                    {
                        aoeRangeTemp = (i + 1) * AOERange / numOfExplode;
                        DealDamage((int)(Damage / numOfExplode), aoeRangeTemp);
                        i++;
                    }, timeExplode / numOfExplode, numOfExplode);
                    break;
                case TYPE.INCREASE_SINGLE_EXPLODE:
                    if (oldTargets == null) oldTargets = new List<T>();
                    i = 0;
                    TimerManager.Ins.TriggerLoopAction(() =>
                    {
                        aoeRangeTemp = (i + 1) * AOERange / numOfExplode;
                        DealDamage((int)(Damage), aoeRangeTemp);
                        i++;
                    }, timeExplode / numOfExplode, numOfExplode);
                    break;
            }
        }

        protected virtual void DealDamage(int damage, float aoeRange)
        { 
            _OnTargetDetection?.Invoke(targets);
        }

        protected virtual void RegisterArea(AREA_TYPE type, List<Cube> cubes, float intensity, float duration, object source)
        { 
            _OnRegisterArea?.Invoke(type, cubes, intensity, duration, source);
        }
        protected virtual void RemoveArea(object source)
        {
            _OnRemoveArea?.Invoke(source);
        }
#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(explodeTF.position, aoeRangeTemp);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(explodeTF.position, AOERange);
        }
#endif
    }
}
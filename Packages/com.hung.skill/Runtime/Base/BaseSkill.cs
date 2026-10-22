using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Hung.Utilities.Timer;

namespace Hung.Skill
{
    public abstract class BaseSkill : MonoBehaviour
    {
        [SerializeField]
        protected Transform tf;
        [SerializeField]
        protected SkillData skillData;
        protected List<Action> actions;
        protected List<float> times;
        protected STimer skillTimer;
        protected STimer activationTimer;
        protected bool isExecute = false;
        protected bool isCanActivation = false;
        protected bool isActivation = false;
        protected int skillLevel;
        protected int skillState;
        protected Type skillType;
        protected int skillRarity;
        public SkillData SkillData => skillData;
        public abstract int MaxTarget { get; set; }
        public virtual int SkillLevel
        {
            get => skillLevel;
            set
            {
                skillLevel = value;
                skillData.Level = skillLevel;
            }
        }
        public virtual int SkillState
        {
            get
            {
                skillState = 0;
                if (skillTimer.IsStart)
                    skillState = -1;
                return skillState;
            }       
        }
        public virtual int SkillRarity
        {
            get => skillRarity;
            set
            {
                skillRarity = value;
                UpdateLevelSkillProperties();
            }
        }
        public Transform Tf => tf;
        public bool IsCanActivation => isCanActivation;
        protected virtual void Awake()
        {
            skillTimer = TimerManager.Ins.PopSTimer();
        }
        
        [Button("Execute")]
        public virtual void SkillExecute()
        {
            isExecute = true;
        }
        public virtual void SkillActivation()
        {
            isActivation = true;
        }
        public virtual void StopActivation()
        {
            isActivation = false;
        }
        public virtual void StopExecute() {}
        protected abstract void UpdateLevelSkillProperties();
    }
}
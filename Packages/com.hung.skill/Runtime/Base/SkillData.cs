using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Skill
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "ScriptableObjects/SkillData/SkillData", order = 1)]
    public class SkillData : ScriptableObject
    {
        [SerializeField]
        string spellName;
        public string SpellName => spellName;
        [SerializeField]
        List<string> normalDescription;
        public string NormalDescription => normalDescription[level];
        [SerializeField]
        List<string> perfectDescription;
        public string PerfectDescription => perfectDescription[level];
        [SerializeField]
        string evolveDescription;
        public string EvolveDescription => evolveDescription;
        protected int level;
        public int Level
        {
            get => level;
            set => level = value == 0 ? 0 : value - 1;
        }
        [SerializeField]
        List<float> coolDowns;
        public float CD => coolDowns[Level];
        [SerializeField]
        List<float> damages;
        public float Damage => damages[Level];
        [SerializeField]
        List<float> aoeRanges;
        public float AoeRange => aoeRanges[Level];
        [SerializeField]
        List<float> critRates;
        public float CritRate => critRates[Level];
        [SerializeField]
        List<float> critDamages;
        public float CritDamage => critDamages[Level];
        [SerializeField]
        List<float> lifeSteals;
        public float LifeSteal => lifeSteals[Level];
        [SerializeField]
        List<int> maxTargets;
        public int MaxTargets => maxTargets[Level];
        [SerializeField]
        List<float> existTimes;
        public float ExistTime => existTimes[Level];
        [SerializeField]
        List<float> speeds;
        public float Speed => speeds[Level];
    }
}

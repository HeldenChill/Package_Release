using Hung.Combat;
using UnityEngine;

using Hung.DesignPattern;

namespace Hung.StatusEffect
{
    public struct StatusAppliedEvent : IEvent
    {
        public IStatusEffectHost Host;
        public GameObject Target;
        public GameObject Caster;
        public StatusEffectTag Tag;
        public StatusApplicationContext Context;
    }
    public struct SynergyStatusAppliedEvent : IEvent
    {
        public IStatusEffectHost Target;
        public GameObject Caster;
        public SynergyDefinitionData Data;
        public StatusApplicationContext Context;
    }
}

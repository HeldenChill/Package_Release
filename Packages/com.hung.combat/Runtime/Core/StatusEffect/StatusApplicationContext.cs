using UnityEngine;

namespace Hung.StatusEffect
{
    public struct StatusApplicationContext
    {
        public GameObject Caster;
        public string CasterId;
        public float Potency;
        public StatusApplySource Source;
        public object SourceDamage;

        public static StatusApplicationContext FromCaster(GameObject caster, StatusApplySource source, float potency = 0f)
        {
            return new StatusApplicationContext
            {
                Caster = caster,
                CasterId = string.Empty,
                Potency = potency,
                Source = source,
                SourceDamage = null,
            };
        }
    }
}

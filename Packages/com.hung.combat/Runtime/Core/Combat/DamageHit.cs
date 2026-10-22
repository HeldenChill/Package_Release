using System.Collections.Generic;
using Hung.Base;

namespace Hung.Combat
{
    public struct DamageHit
    {
        public float Amount;
        public ELEMENTAL[] Types;
        public float CritChance;
        public float CritDamage;
        public bool ForceCrit;
        public object Source;
        public bool SuppressElementalProc;
        public IReadOnlyList<global::StatusEffect> OnHitTemplates;
        public string CasterId;
        public bool OnHitTemplatesDelayPreBaked;
        // Consumed by Enemy.ApplyPostDamageEffects to override elemental proc rates per hit.
        public IHitProcProfile ProcProfile;
    }
}

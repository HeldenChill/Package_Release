using Hung.Base;

namespace Hung.Combat
{
    public static class DamageHitBuilders
    {
        public static DamageHit From(float amount, ELEMENTAL element, object source = null) => new DamageHit
        {
            Amount = amount,
            Types = new[] { element },
            CritChance = 0f,
            CritDamage = 0f,
            Source = source,
        };

        public static DamageHit From(float amount, ELEMENTAL[] types, object source = null) => new DamageHit
        {
            Amount = amount,
            Types = types,
            CritChance = 0f,
            CritDamage = 0f,
            Source = source,
        };

        public static DamageHit Melee(float amount, object source = null) =>
            From(amount, ELEMENTAL.MELEE, source);

        public static DamageHit FromAttackStats(
            float amount,
            ELEMENTAL[] types,
            float critChance,
            float critDamage,
            object source = null) => new DamageHit
        {
            Amount = amount,
            Types = types,
            CritChance = critChance,
            CritDamage = critDamage,
            Source = source,
        };
    }
}

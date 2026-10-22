using System;
using Hung.Base;

namespace Hung.Combat
{
    public static class DamagePipeline
    {
        public static FinalDamageInfo Calculate(in DamageHit hit, IElementalResistanceProfile baseProfile, in DamageDealtContext context)
        {
            if (IsHasDamageType(hit, ELEMENTAL.POISON))
            {
                ELEMENTAL mainType = ELEMENTAL.POISON;

                if (!context.SuppressElementalProc)
                {
                    return new FinalDamageInfo(0, mainType);
                }
                return new FinalDamageInfo(hit.Amount, mainType);
            }

            IElementalResistanceProfile profile = DamageModifierRegistry.ApplyModifiers(context, baseProfile);
            return DamageCalculator.Calculate(hit, profile);
        }

        private static bool IsHasDamageType(in DamageHit hit, ELEMENTAL type)
        {
            return hit.Types != null && hit.Types.Length > 0 && Array.Exists(hit.Types, t => t == type);
        }
    }
}

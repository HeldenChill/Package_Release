using System.Collections.Generic;
using Hung.Base;
using Hung.Combat;

public static class DamageCalculator
{
    static readonly Dictionary<ELEMENTAL, System.Func<IElementalResistanceProfile, float>> TypeMultiplierGetters = new()
    {
        { ELEMENTAL.RANGE, p => p.GetMultiplier(ELEMENTAL.RANGE) },
        { ELEMENTAL.MELEE, p => p.GetMultiplier(ELEMENTAL.MELEE) },
        { ELEMENTAL.FIRE, p => p.GetMultiplier(ELEMENTAL.FIRE) },
        { ELEMENTAL.ICE, p => p.GetMultiplier(ELEMENTAL.ICE) },
        { ELEMENTAL.ELECTRIC, p => p.GetMultiplier(ELEMENTAL.ELECTRIC) },
        { ELEMENTAL.POISON, p => p.GetMultiplier(ELEMENTAL.POISON) },
        { ELEMENTAL.ARCANE, p => p.GetMultiplier(ELEMENTAL.ARCANE) },
    };

    // Higher value = higher priority when multipliers are equal: POISON > ARCANE > FIRE > ELECTRIC > ICE > MELEE = RANGE
    static readonly Dictionary<ELEMENTAL, int> TypePriority = new()
    {
        { ELEMENTAL.POISON, 6 },
        { ELEMENTAL.ARCANE, 5 },
        { ELEMENTAL.FIRE, 4 },
        { ELEMENTAL.ELECTRIC, 3 },
        { ELEMENTAL.ICE, 2 },
        { ELEMENTAL.MELEE, 1 },
        { ELEMENTAL.RANGE, 1 },
    };

    public static FinalDamageInfo Calculate(in DamageHit hit, IElementalResistanceProfile resistance)
    {
        if (hit.Types == null || hit.Types.Length == 0 || resistance == null)
        {
            return new FinalDamageInfo(0, ELEMENTAL.RANGE);
        }

        float typeMult = 0;
        float mainTypeMult = -1f;
        ELEMENTAL mainType = ELEMENTAL.RANGE;
        foreach (ELEMENTAL type in hit.Types)
        {
            if (!TypeMultiplierGetters.TryGetValue(type, out var getMultiplier))
            {
                continue;
            }

            float currentMult = getMultiplier(resistance);
            typeMult += currentMult;

            if (currentMult > mainTypeMult ||
                (currentMult == mainTypeMult && TypePriority.GetValueOrDefault(type) > TypePriority.GetValueOrDefault(mainType)))
            {
                mainTypeMult = currentMult;
                mainType = type;
            }
        }

        float baseDamage = hit.Amount / hit.Types.Length * typeMult;
        bool isCrit = hit.ForceCrit || UnityEngine.Random.value < hit.CritChance;
        // Degenerate guard: unset/<=1 CritDamage gives no crit benefit, so do not report a crit.
        isCrit = isCrit && hit.CritDamage > 1f;
        float critMultiplier = isCrit ? hit.CritDamage : 1f;
        float finalDmg = baseDamage * critMultiplier;

        return new FinalDamageInfo(finalDmg, mainType, isCrit);
    }

    public static FinalDamageInfo Calculate(float damage, List<ELEMENTAL> types, IElementalResistanceProfile resistance, bool forceCrit = false, float critChance = 0f, float critDamage = 1f)
    {
        if (types == null || types.Count == 0 || resistance == null)
        {
            return new FinalDamageInfo(0, ELEMENTAL.RANGE);
        }

        float typeMult = 0;
        float mainTypeMult = -1f;
        ELEMENTAL mainType = ELEMENTAL.RANGE;
        int typeCount = types.Count;

        foreach (var type in types)
        {
            if (!TypeMultiplierGetters.TryGetValue(type, out var getMultiplier))
            {
                continue;
            }

            float currentMult = getMultiplier(resistance);
            typeMult += currentMult;

            if (currentMult > mainTypeMult ||
                (currentMult == mainTypeMult && TypePriority.GetValueOrDefault(type) > TypePriority.GetValueOrDefault(mainType)))
            {
                mainTypeMult = currentMult;
                mainType = type;
            }
        }

        float baseDamage = damage / typeCount * typeMult;
        bool isCrit = forceCrit || UnityEngine.Random.value < critChance;
        // Degenerate guard: unset/<=1 critDamage gives no crit benefit, so do not report a crit.
        isCrit = isCrit && critDamage > 1f;
        float finalDmg = baseDamage * (isCrit ? critDamage : 1f);
        return new FinalDamageInfo(finalDmg, mainType, isCrit);
    }
}

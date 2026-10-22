using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public class BurnElectricBurstReaction : ISynergyReaction
{
    public string ReactionId => "BurnElectric_Burst";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null || !target.TryGetCombatDamageable(out ICombatDamageable damageable))
        {
            return;
        }

        float mult = 0.25f;
        if (definition.Parameters != null && definition.Parameters.Length > 0)
        {
            mult = definition.Parameters[0];
        }

        float remainingBurnDamage = StatusEffectServiceLocator.ComputeRemainingBurnDotDamage(target);
        if (remainingBurnDamage <= 0f)
        {
            return;
        }

        StatusEffectServiceLocator.ConsumeAllBurnStacks(target);

        damageable.TakeDamage(new DamageHit
        {
            Amount = remainingBurnDamage * mult,
            Types = new[] { ELEMENTAL.ELECTRIC },
            CritChance = 0f,
            CritDamage = 0f,
            Source = caster,
            SuppressElementalProc = true,
        });
    }
}

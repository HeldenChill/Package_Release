using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

[System.Serializable]
public class BurnStatusEffect : StatusEffect
{
    public const float DefaultDurationSeconds = 3f;
    public const float MaxHpDotPercentPerSecond = 0.02f;
    public const float DefaultTickIntervalSeconds = 1f;

    public override void OnApply(IStatusEffectHost target, GameObject caster)
    {
        base.OnApply(target, caster);
        target.VisualReceiver?.OnStatusStackApplied(StatusEffectTag.Burn);
    }

    public override void OnTick(IStatusEffectHost target, GameObject caster)
    {
        if (target == null || !target.TryGetMaxHealth(out float maxHp)
            || !target.TryGetCombatDamageable(out ICombatDamageable damageable))
        {
            return;
        }

        float tickDamage = maxHp * MaxHpDotPercentPerSecond;
        if (tickDamage <= 0f)
        {
            return;
        }

        damageable.TakeDamage(new DamageHit
        {
            Amount = tickDamage,
            Types = new[] { ELEMENTAL.FIRE },
            CritChance = 0f,
            CritDamage = 0f,
            Source = caster,
            SuppressElementalProc = true,
        });
    }

    public override void OnRemove(IStatusEffectHost target, GameObject caster)
    {
        target.VisualReceiver?.OnStatusStackRemoved(StatusEffectTag.Burn);
        base.OnRemove(target, caster);
    }

    public override StatusEffect Clone()
    {
        var clone = new BurnStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }

    public static BurnStatusEffect CreateTemplate(float durationSeconds = DefaultDurationSeconds)
    {
        return new BurnStatusEffect
        {
            statusName = "Burn",
            duration = durationSeconds,
            tickInterval = DefaultTickIntervalSeconds,
            canStack = true,
            maxStacks = 999,
            statusType = StatusEffectType.Burn,
        };
    }
}

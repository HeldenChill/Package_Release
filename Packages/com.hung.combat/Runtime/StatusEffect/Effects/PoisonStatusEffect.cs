using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

[System.Serializable]
public class PoisonStatusEffect : StatusEffect, IAggregateStatusTickEffect, ISingleInstanceStackable
{
    // ISingleInstanceStackable
    public int StackCount => baseDamage <= 0f ? 0 : Mathf.Max(1, (int)baseDamage);
    public int MaxStacks => maxStacks;
    public const float DefaultTickIntervalSeconds = 2f;
    public const float DefaultDamagePerStack = 1f;
    public const float DefaultDecayFraction = 0.5f;

    public static float TickIntervalSeconds { get; set; } = DefaultTickIntervalSeconds;
    public static float DamagePerStack { get; set; } = DefaultDamagePerStack;
    public static float DecayFraction { get; set; } = DefaultDecayFraction;
    public void PushStacks(int count)
    {
        baseDamage = Mathf.Min(baseDamage + count, maxStacks);
    }
    public int PopStacks(int count)
    {
        int actual = (int)Mathf.Min(count, baseDamage);
        baseDamage -= actual;
        return actual;
    }
    public void OnAggregateTick(IStatusEffectHost target, GameObject caster)
    {
        if (target == null || !target.TryGetCombatDamageable(out ICombatDamageable damageable))
        {
            return;
        }

        int currentStacks = StatusEffectServiceLocator.GetStackCount(target, StatusEffectType.Poison);
        if (currentStacks <= 0)
        {
            return;
        }

        float tickDamage = currentStacks * DamagePerStack;
        if (tickDamage <= 0f)
        {
            return;
        }
        int removeCount = currentStacks <= 1
                    ? 1
                    : Mathf.FloorToInt(currentStacks * DecayFraction);

        if (removeCount > 0)
        {
            StatusEffectServiceLocator.ConsumeStacks(target, StatusEffectType.Poison, removeCount);
        }
        damageable.TakeDamage(new DamageHit
        {
            Amount = tickDamage,
            Types = new[] { ELEMENTAL.POISON },
            CritChance = 0f,
            CritDamage = 0f,
            Source = caster,
            SuppressElementalProc = true,
        });


    }

    public override StatusEffect Clone()
    {
        var clone = new PoisonStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }
}

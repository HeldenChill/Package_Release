using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    public string statusName;
    public float duration;
    public float tickInterval = 1f;
    public bool canStack = true;
    public int maxStacks = 10;
    public float delayTime = -1;
    public float tempDelayTime = 0;
    public StatusEffectType statusType;
    public float baseDamage;
    public float damageRatio;
    public DamageType damageType;
    public float triggerChance;
    protected IModStat statusTarget;

    public float damage
    {
        get
        {
            float resultDamage = baseDamage;
            if (damageType == DamageType.AdditionalDamagePercent)
            {
                resultDamage = baseDamage * damageRatio;
            }

            return resultDamage;
        }
    }

    public virtual void OnApply(IStatusEffectHost target, GameObject caster)
    {
        statusTarget = target;
    }

    public virtual void OnTick(IStatusEffectHost target, GameObject caster) { }

    public virtual void OnRemove(IStatusEffectHost target, GameObject caster)
    {
        statusTarget = null;
    }

    public virtual bool IsTargetAvailable =>
        statusTarget != null && statusTarget.IsStatusTargetAlive;

    public virtual StatusEffect Clone() => null;

    protected void CopyBaseFields(StatusEffect source)
    {
        statusName = source.statusName;
        duration = source.duration;
        tickInterval = source.tickInterval;
        canStack = source.canStack;
        maxStacks = source.maxStacks;
        delayTime = source.delayTime;
        tempDelayTime = source.tempDelayTime;
        statusType = source.statusType;
        baseDamage = source.baseDamage;
        damageRatio = source.damageRatio;
        damageType = source.damageType;
        triggerChance = source.triggerChance;
    }
}

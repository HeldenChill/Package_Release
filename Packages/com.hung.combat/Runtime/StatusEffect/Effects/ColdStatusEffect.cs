using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using SStats;
using UnityEngine;

[System.Serializable]
public class ColdStatusEffect : StatusEffect
{
    StatModifier speedModifier;

    public override void OnApply(IStatusEffectHost target, GameObject caster)
    {
        base.OnApply(target, caster);
        speedModifier = new StatModifier(-0.05f, StatModType.PercentAdd);
        target.AddStatModifier(STAT.SPEED, speedModifier);
        target.VisualReceiver.OnStatusStackApplied(StatusEffectTag.Cold);
    }

    public override void OnRemove(IStatusEffectHost target, GameObject caster)
    {
        if (speedModifier != null)
        {
            target.RemoveStatModifier(STAT.SPEED, speedModifier);
            target.VisualReceiver.OnStatusStackRemoved(StatusEffectTag.Cold);
            speedModifier = null;
        }

        base.OnRemove(target, caster);
    }

    public override StatusEffect Clone()
    {
        var clone = new ColdStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }
}

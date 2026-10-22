using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

[System.Serializable]
public class OverloadStatusEffect : StatusEffect
{
    public override void OnApply(IStatusEffectHost target, GameObject caster)
    {
        base.OnApply(target, caster);

        if (target is IStatusStunnable stunnable)
        {
            stunnable.SetStunned(true);
            target.VisualReceiver?.OnStatusStackApplied(StatusEffectTag.Overload);
        }
    }
    public override void OnRemove(IStatusEffectHost target, GameObject caster)
    {

        if (target is IStatusStunnable stunnable)
        {
            stunnable.SetStunned(false);
            target.VisualReceiver?.OnStatusStackRemoved(StatusEffectTag.Overload);
        }
        base.OnRemove(target, caster);

    }

    public override StatusEffect Clone()
    {
        var clone = new OverloadStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }
}

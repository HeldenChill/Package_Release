using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

[System.Serializable]
public class BleedStatusEffect : StatusEffect
{
    public const float DefaultDamageTakenPerStack = 0.05f;
    public const int DefaultMaxStacks = 30;

    public static float DamageTakenPerStack { get; set; } = DefaultDamageTakenPerStack;
    public static int MaxStacks { get; set; } = DefaultMaxStacks;

    public override void OnApply(IStatusEffectHost target, GameObject caster)
    {
        base.OnApply(target, caster);
        if (target.TryGetDamageTakenMultiplierReceiver(out IDamageTakenMultiplierReceiver receiver))
        {
            receiver.AddDamageTakenPercentAddAllTypes(DamageTakenPerStack, this);
        }
        target.VisualReceiver?.OnStatusStackApplied(StatusEffectTag.Bleed);
    }

    public override void OnRemove(IStatusEffectHost target, GameObject caster)
    {
        if (target.TryGetDamageTakenMultiplierReceiver(out IDamageTakenMultiplierReceiver receiver))
        {
            receiver.RemoveDamageTakenPercentAddFromSourceAllTypes(this);
        }
        target.VisualReceiver?.OnStatusStackRemoved(StatusEffectTag.Bleed);
        base.OnRemove(target, caster);
    }

    public override StatusEffect Clone()
    {
        var clone = new BleedStatusEffect();
        clone.CopyBaseFields(this);
        return clone;
    }
}

using Hung.Combat;
using Hung.StatusEffect;
using Hung.DesignPattern;
using UnityEngine;

public class ColdSpreadOnDeathReaction : ISynergyReaction
{
    public string ReactionId => "ColdSpread_OnDeath";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null) return;

        float threshold = definition.Parameters != null && definition.Parameters.Length >= 4
            ? definition.Parameters[3]
            : 10f;

        int coldStacks = StatusEffectServiceLocator.GetStackCount(target, StatusEffectType.Cold);
        if (coldStacks < threshold) return;

        EventBus<SynergyStatusAppliedEvent>.Raise(new SynergyStatusAppliedEvent
        {
            Target = target,
            Caster = caster,
            Data = definition,
            Context = context,
        });
    }
}

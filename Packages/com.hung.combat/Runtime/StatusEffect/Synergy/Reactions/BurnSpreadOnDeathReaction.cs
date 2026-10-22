using Hung.Combat;
using Hung.StatusEffect;
using Hung.DesignPattern;
using UnityEngine;

public class BurnSpreadOnDeathReaction : ISynergyReaction
{
    public string ReactionId => "BurnSpread_OnDeath";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null) return;
        if (!StatusEffectServiceLocator.HasActiveTag(target, StatusEffectTag.Burn)) return;

        EventBus<SynergyStatusAppliedEvent>.Raise(new SynergyStatusAppliedEvent
        {
            Target = target,
            Caster = caster,
            Data = definition,
            Context = context,
        });
    }
}

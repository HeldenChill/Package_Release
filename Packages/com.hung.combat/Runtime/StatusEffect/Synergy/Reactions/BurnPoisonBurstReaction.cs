using Hung.Combat;
using Hung.StatusEffect;
using Hung.DesignPattern;
using UnityEngine;

public class BurnPoisonBurstReaction : ISynergyReaction
{
    public string ReactionId => "PoisonSpread_Burst";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null) return;

        int consumeStack = StatusEffectServiceLocator.GetStackCount(target, StatusEffectType.Poison);
        if (consumeStack <= 0) return;
        consumeStack = consumeStack / 2; // Cap the burst at 10 stacks for balance

        StatusEffectServiceLocator.ConsumeStacks(target, StatusEffectType.Poison, consumeStack);

        var modifiedDefinition = definition;
        float radius = definition.Parameters.Length > 0 ? definition.Parameters[0] : 1f;
        modifiedDefinition.Parameters = new float[] { radius, consumeStack };

        EventBus<SynergyStatusAppliedEvent>.Raise(new SynergyStatusAppliedEvent
        {
            Target = target,
            Caster = caster,
            Data = modifiedDefinition,
            Context = context,
        });
    }
}

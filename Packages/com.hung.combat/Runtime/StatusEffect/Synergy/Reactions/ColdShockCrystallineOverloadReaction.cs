using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public class ColdShockCrystallineOverloadReaction : ISynergyReaction
{
    public string ReactionId => "ColdShock_CrystallineOverload";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null)
        {
            return;
        }

        int stackMultiplier = 3;
        if (definition.Parameters != null && definition.Parameters.Length > 0)
        {
            stackMultiplier = Mathf.RoundToInt(definition.Parameters[0]);
        }

        StatusEffectServiceLocator.RemoveAllOfType(target, typeof(ColdStatusEffect));
        StatusEffectServiceLocator.MultiplyStacks(target, StatusEffectType.Shock, stackMultiplier, caster, context);
        target.VisualReceiver?.OnSynergyStatusTrigger(SynergyEffectTag.StaticCold);
    }
}

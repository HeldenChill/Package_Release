using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public class BurnElectricExtendReaction : ISynergyReaction
{
    public string ReactionId => "BurnElectric_Extend";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null)
        {
            return;
        }

        float extensionSeconds = 2f;
        if (definition.Parameters != null && definition.Parameters.Length > 0)
        {
            extensionSeconds = definition.Parameters[0];
        }

        StatusEffectServiceLocator.ExtendDuration(target, StatusEffectType.Burn, extensionSeconds);
        target.VisualReceiver?.OnSynergyStatusTrigger(SynergyEffectTag.ElectricBurn);
    }
}
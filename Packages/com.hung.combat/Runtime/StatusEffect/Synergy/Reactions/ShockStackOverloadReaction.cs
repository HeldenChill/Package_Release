using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public class ShockStackOverloadReaction : ISynergyReaction
{
    public string ReactionId => "ShockStack10_Overload";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null)
        {
            return;
        }

        GameObject targetGo = target.GetGameObject();
        if (targetGo == null)
        {
            return;
        }

        int threshold = 10;
        if (definition.Parameters != null && definition.Parameters.Length > 0)
        {
            threshold = Mathf.Max(1, Mathf.RoundToInt(definition.Parameters[0]));
        }

        int shockCount = StatusEffectServiceLocator.GetCount(targetGo, typeof(ShockStatusEffect));
        if (shockCount == 0 || shockCount % threshold != 0)
        {
            return;
        }

        float duration = 0.5f;
        if (definition.Parameters != null && definition.Parameters.Length > 1)
        {
            duration = definition.Parameters[1];
        }

        var overload = StatusEffectFactory.Create(StatusEffectType.Overload, duration);
        if (overload == null)
        {
            return;
        }

        StatusEffectServiceLocator.Apply(overload, targetGo, StatusApplicationContext.FromCaster(caster, StatusApplySource.Synergy));
    }
}

using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public class ApplyStatusReaction : ISynergyReaction
{
    public string ReactionId => "ApplyStatus";

    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        GameObject targetGo = target?.GetGameObject();
        if (targetGo == null || definition.Parameters == null || definition.Parameters.Length < 2)
        {
            return;
        }

        int statusIndex = Mathf.RoundToInt(definition.Parameters[0]);
        float duration = definition.Parameters.Length > 1 ? definition.Parameters[1] : 1f;

        StatusEffectType type = (StatusEffectType)statusIndex;
        StatusEffect effect = StatusEffectFactory.Create(type, duration);
        if (effect == null)
        {
            return;
        }

        var synergyContext = context;
        synergyContext.Source = StatusApplySource.Synergy;
        StatusEffectServiceLocator.Apply(effect, targetGo, synergyContext);
    }
}

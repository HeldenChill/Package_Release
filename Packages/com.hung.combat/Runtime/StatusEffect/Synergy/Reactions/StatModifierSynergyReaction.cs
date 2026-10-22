using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using SStats;
using UnityEngine;

public class StatModifierSynergyReaction : ISynergyReaction
{
    public string ReactionId => "StatModifier";

    /// <summary>
    /// Applies a persistent stat modifier to the target via IModStat interface.
    /// Expected definition.Parameters: [statEnum, modType, modValue]
    /// E.g., Parameters[0] = (int)STAT.AttackPower, Parameters[1] = (int)StatModType.PercentAdd, Parameters[2] = 0.25f
    /// </summary>
    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null || definition.Parameters == null || definition.Parameters.Length < 3)
        {
            Debug.LogWarning($"StatModifierSynergyReaction: Invalid parameters for synergy '{definition.SynergyId}'");
            return;
        }

        // Extract parameters
        int statIndex = Mathf.RoundToInt(definition.Parameters[0]);
        int modTypeIndex = Mathf.RoundToInt(definition.Parameters[1]);
        float modValue = definition.Parameters[2];

        if (!System.Enum.IsDefined(typeof(STAT), statIndex))
        {
            Debug.LogWarning($"StatModifierSynergyReaction: Invalid STAT enum value {statIndex}");
            return;
        }

        if (!System.Enum.IsDefined(typeof(StatModType), modTypeIndex))
        {
            Debug.LogWarning($"StatModifierSynergyReaction: Invalid StatModType enum value {modTypeIndex}");
            return;
        }

        STAT stat = (STAT)statIndex;
        StatModType modType = (StatModType)modTypeIndex;

        // Create modifier with synergy as source for later cleanup
        var modifier = new StatModifier(modValue, modType, (int)modType, $"Synergy_{definition.SynergyId}");

        // Apply via IModStat if target supports it
        var modStatTarget = target as IModStat;
        if (modStatTarget != null && modStatTarget.IsStatusTargetAlive)
        {
            modStatTarget.AddStatModifier(stat, modifier, isShowUI: true);

            Debug.Log($"StatModifierSynergyReaction: Applied {modType} {modValue} to {stat} on {target.GetType().Name} (Synergy: {definition.SynergyId})");
        }
        else
        {
            Debug.LogWarning($"StatModifierSynergyReaction: Target does not implement IModStat or is dead");
        }
    }
}

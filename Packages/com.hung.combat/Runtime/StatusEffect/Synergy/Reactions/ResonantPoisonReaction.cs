using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using SStats;
using UnityEngine;

/// <summary>
/// ResonantPoisonReaction: Applying a status effect to an enemy affected by Poison
/// increases the caster's Crit Rate by 5% and Crit Damage by 50%.
/// The bonuses do not stack — only one instance applies regardless of Poison stacks.
/// </summary>
public class ResonantPoisonReaction : ISynergyReaction
{
    public string ReactionId => "ResonantPoison";

    private const string MODIFIER_SOURCE_ID = "Synergy_ResonantPoison";
    private const float CRIT_RATE_BONUS = 0.05f;  // 5%
    private const float CRIT_DAMAGE_BONUS = 0.5f;  // 50%
    private StatModifier critRateModifier;
    private StatModifier critDamageModifier;
    public ResonantPoisonReaction()
    {
        critRateModifier = new StatModifier(CRIT_RATE_BONUS, StatModType.PercentAdd, 1, MODIFIER_SOURCE_ID);
        critDamageModifier = new StatModifier(CRIT_DAMAGE_BONUS, StatModType.PercentAdd, 1, MODIFIER_SOURCE_ID);
    }
    public void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType)
    {
        if (target == null || caster == null || triggeringType != StatusEffectTag.Poison)
        {
            return;
        }
        // Check if target has Poison status effect
        int poisonStackCount = StatusEffectServiceLocator.GetStackCount(target, StatusEffectType.Poison);
        if (poisonStackCount <= 0)
        {
            return;
        }

        // Apply stat modifiers to caster if alive
        if (target != null && target.IsStatusTargetAlive)
        {
            // Create modifiers with unique source ID to prevent stacking
            target.AddStatModifier(STAT.TARGET_CRIT_BONUS, critRateModifier, isShowUI: true);
            target.AddStatModifier(STAT.TARGET_CRIT_DMG_BONUS, critDamageModifier, isShowUI: true);

            Debug.Log($"ResonantPoisonReaction: Applied +{CRIT_RATE_BONUS * 100:F0}% Crit Rate and +{CRIT_DAMAGE_BONUS * 100:F0}% Crit Damage to {caster.name} (Target had {poisonStackCount} Poison stacks)");
        }
        else
        {
            Debug.LogWarning($"ResonantPoisonReaction: Caster does not implement IModStat or is dead");
        }
    }

    public void OnStatusRemoved(IStatusEffectHost target, StatusEffectType removedType, GameObject caster)
    {
        if (removedType != StatusEffectType.Poison || target == null) return;

        // If Burn is still active, Poison was consumed by the synergy burst (BurnPoisonBurst),
        // not by natural expiry. Keep the modifier so it outlives the burst frame.
        int burnStacks = StatusEffectServiceLocator.GetStackCount(target, StatusEffectType.Burn);
        if (burnStacks > 0) return;

        target.RemoveStatModifier(STAT.TARGET_CRIT_BONUS, critRateModifier, isShowUI: true);
        target.RemoveStatModifier(STAT.TARGET_CRIT_DMG_BONUS, critDamageModifier, isShowUI: true);

        var go = target.GetGameObject();
        Debug.Log($"ResonantPoisonReaction: Removed Crit bonuses from {(go != null ? go.name : "null")} (Poison expired)");
    }
}

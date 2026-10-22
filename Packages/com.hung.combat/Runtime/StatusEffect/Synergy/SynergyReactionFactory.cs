using System.Collections.Generic;
using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public static class SynergyReactionFactory
{
    static readonly Dictionary<string, ISynergyReaction> Reactions = new();

    static SynergyReactionFactory()
    {
        Register(new ApplyStatusReaction());
        Register(new ResonantPoisonReaction());
        Register(new ShockStackOverloadReaction());
        Register(new BurnElectricExtendReaction());
        Register(new BurnSpreadOnDeathReaction());
        Register(new ColdShockCrystallineOverloadReaction());
        Register(new BurnPoisonBurstReaction());
        Register(new ColdSpreadOnDeathReaction());
        Register(new StatModifierSynergyReaction());
    }

    public static void Register(ISynergyReaction reaction)
    {
        if (reaction == null || string.IsNullOrEmpty(reaction.ReactionId))
        {
            return;
        }

        Reactions[reaction.ReactionId] = reaction;
    }

    public static bool TryGet(string reactionId, out ISynergyReaction reaction) =>
        Reactions.TryGetValue(reactionId, out reaction);

    public static void NotifyRemoved(IStatusEffectHost target, StatusEffectType removedType, GameObject caster)
    {
        foreach (ISynergyReaction reaction in Reactions.Values)
        {
            reaction.OnStatusRemoved(target, removedType, caster);
        }
    }
}

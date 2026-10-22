using System.Collections.Generic;
using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public class SynergyResolver
{
    readonly StatusEffectManager manager;
    readonly List<ISynergyReaction> customReactions = new();
    readonly Dictionary<IStatusEffectHost, Dictionary<string, float>> cooldowns = new();

    ISynergyUnlockProvider unlockProvider;
    List<SynergyDefinitionData> definitions = new();

    public SynergyResolver(StatusEffectManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// Initialize default reactions.
    /// Default reactions (including StatModifierSynergyReaction) are registered in SynergyReactionFactory.
    /// Call this method only if custom reactions need to be registered at runtime.
    /// </summary>
    public void InitializeDefaultReactions()
    {
        // Default reactions already registered in SynergyReactionFactory static constructor.
    }

    public void SetUnlockProvider(ISynergyUnlockProvider provider) => unlockProvider = provider;

    public void SetDefinitions(IReadOnlyList<SynergyDefinitionData> defs)
    {
        definitions = defs != null ? new List<SynergyDefinitionData>(defs) : new List<SynergyDefinitionData>();
    }

    public void RegisterReaction(ISynergyReaction reaction)
    {
        if (reaction != null)
        {
            customReactions.Add(reaction);
            SynergyReactionFactory.Register(reaction);
        }
    }

    public void OnStatusApplied(IStatusEffectHost target, GameObject caster, StatusEffectTag appliedTag, StatusApplicationContext context)
    {
        if (target == null || definitions.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<StatusEffectTag> activeTags = manager.GetActiveTags(target);

        for (int i = 0; i < definitions.Count; i++)
        {
            SynergyDefinitionData def = definitions[i];
            if (def.TriggerType != SynergyTriggerType.OnStatusApplied)
            {
                continue;
            }

            if (!HasRequiredTags(activeTags, appliedTag, def.RequiredTags))
            {
                continue;
            }

            if (!IsUnlocked(def, caster, context))
            {
                continue;
            }

            if (IsOnCooldown(target, def.SynergyId, def.CooldownPerTarget))
            {
                continue;
            }

            if (!TryResolveReaction(def.ReactionId, out ISynergyReaction reaction))
            {
                continue;
            }

            reaction.Execute(def, target, caster, context, appliedTag);
            if (def.CooldownPerTarget > 0f)
            {
                SetCooldown(target, def.SynergyId, Time.time);
            }
        }
    }

    public void OnStatusRemoved(IStatusEffectHost target, StatusEffectType removedType, GameObject caster)
    {
        if (target == null) return;
        foreach (ISynergyReaction reaction in customReactions)
            reaction.OnStatusRemoved(target, removedType, caster);
        SynergyReactionFactory.NotifyRemoved(target, removedType, caster);
    }

    public void OnHostDefeated(IStatusEffectHost target, GameObject caster)
    {
        if (target == null || definitions.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<StatusEffectTag> activeTags = manager.GetActiveTags(target);

        for (int i = 0; i < definitions.Count; i++)
        {
            SynergyDefinitionData def = definitions[i];
            if (def.TriggerType != SynergyTriggerType.OnHostDefeated)
            {
                continue;
            }

            if (!HasRequiredTags(activeTags, default, def.RequiredTags))
            {
                continue;
            }

            if (!IsUnlocked(def, caster, default))
            {
                continue;
            }

            if (IsOnCooldown(target, def.SynergyId, def.CooldownPerTarget))
            {
                continue;
            }

            if (!TryResolveReaction(def.ReactionId, out ISynergyReaction reaction))
            {
                continue;
            }

            reaction.Execute(def, target, caster, default, default);
            if (def.CooldownPerTarget > 0f)
            {
                SetCooldown(target, def.SynergyId, Time.time);
            }
        }
    }

    static bool HasRequiredTags(
        IReadOnlyCollection<StatusEffectTag> activeTags,
        StatusEffectTag justApplied,
        StatusEffectTag[] required)
    {
        if (required == null || required.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < required.Length; i++)
        {
            bool found = false;
            foreach (StatusEffectTag tag in activeTags)
            {
                if (tag == required[i])
                {
                    found = true;
                    break;
                }
            }

            if (!found && justApplied != required[i])
            {
                return false;
            }
        }

        return true;
    }

    bool IsUnlocked(SynergyDefinitionData def, GameObject caster, StatusApplicationContext context)
    {
        if (string.IsNullOrEmpty(def.UnlockKey))
        {
            return true;
        }

        if (unlockProvider == null)
        {
            return false;
        }

        string casterId = context.CasterId;
        if (string.IsNullOrEmpty(casterId) && caster != null)
        {
            casterId = caster.name;
        }

        return unlockProvider.IsUnlocked(
            def.UnlockKey,
            def.UnlockScope,
            new SynergyUnlockContext
            {
                CasterId = casterId,
                UnlockKey = def.UnlockKey,
            });
    }

    bool TryResolveReaction(string reactionId, out ISynergyReaction reaction)
    {
        for (int i = 0; i < customReactions.Count; i++)
        {
            if (customReactions[i].ReactionId == reactionId)
            {
                reaction = customReactions[i];
                return true;
            }
        }

        return SynergyReactionFactory.TryGet(reactionId, out reaction);
    }

    public bool TryGetCooldown(IStatusEffectHost target, string synergyId, out float lastTime)
    {
        lastTime = 0f;
        if (target == null || !cooldowns.TryGetValue(target, out Dictionary<string, float> perTarget))
        {
            return false;
        }

        return perTarget.TryGetValue(synergyId, out lastTime);
    }

    public void SetCooldown(IStatusEffectHost target, string synergyId, float time)
    {
        if (target == null)
        {
            return;
        }

        if (!cooldowns.TryGetValue(target, out Dictionary<string, float> perTarget))
        {
            perTarget = new Dictionary<string, float>();
            cooldowns[target] = perTarget;
        }

        perTarget[synergyId] = time;
    }

    bool IsOnCooldown(IStatusEffectHost target, string synergyId, float cooldownSeconds)
    {
        if (cooldownSeconds <= 0f)
        {
            return false;
        }

        if (!TryGetCooldown(target, synergyId, out float lastTime))
        {
            return false;
        }

        return Time.time - lastTime < cooldownSeconds;
    }
}

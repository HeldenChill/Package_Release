using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using Hung.DesignPattern;
using UnityEngine;
using Hung.Utilities.Timer;

public class StatusEffectManager : Singleton<StatusEffectManager>, IStatusEffectService
{
    readonly Dictionary<IStatusEffectHost, List<ActiveStatusEffect>> activeStatusEffects = new();
    readonly Dictionary<IStatusEffectHost, Dictionary<string, float>> synergyCooldowns = new();
    readonly Dictionary<IStatusEffectHost, ShockOverloadState> shockOverloadStates = new();
    readonly HashSet<IStatusEffectHost> deferredClearHosts = new();
    readonly Dictionary<IStatusEffectHost, HashSet<string>> deferredRemoveById = new();
    bool isUpdating;

    int shockFirstOverloadThreshold = 100;
    float shockOverloadThresholdMultiplier = 2f;
    float shockOverloadDuration = 1f;

    SynergyResolver synergyResolver;

    public void Configure(StatusEffectModuleConfigSO moduleConfig)
    {
        if (moduleConfig == null)
        {
            return;
        }

        shockFirstOverloadThreshold = Mathf.Max(1, moduleConfig.ShockFirstOverloadThreshold);
        shockOverloadThresholdMultiplier = Mathf.Max(1f, moduleConfig.ShockOverloadThresholdMultiplier);
        shockOverloadDuration = Mathf.Max(0.01f, moduleConfig.ShockOverloadDuration);

        PoisonStatusEffect.TickIntervalSeconds = moduleConfig.PoisonTickInterval > 0f
            ? moduleConfig.PoisonTickInterval
            : PoisonStatusEffect.DefaultTickIntervalSeconds;
        PoisonStatusEffect.DamagePerStack = moduleConfig.PoisonDamagePerStack > 0f
            ? moduleConfig.PoisonDamagePerStack
            : PoisonStatusEffect.DefaultDamagePerStack;
        PoisonStatusEffect.DecayFraction = moduleConfig.PoisonDecayFraction > 0f
            ? moduleConfig.PoisonDecayFraction
            : PoisonStatusEffect.DefaultDecayFraction;

        BleedStatusEffect.DamageTakenPerStack = moduleConfig.BleedDamageTakenPerStack > 0f
            ? moduleConfig.BleedDamageTakenPerStack
            : BleedStatusEffect.DefaultDamageTakenPerStack;
        BleedStatusEffect.MaxStacks = moduleConfig.BleedMaxStacks > 0
            ? moduleConfig.BleedMaxStacks
            : BleedStatusEffect.DefaultMaxStacks;
    }

    public void ConfigureSynergy(SynergyResolver resolver)
    {
        synergyResolver = resolver;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    void Update()
    {
        UpdateActiveStatusEffects();
    }

    public void Apply(object statusEffectTemplate, GameObject target, GameObject caster) =>
        Apply(statusEffectTemplate, target, StatusApplicationContext.FromCaster(caster, StatusApplySource.Unknown));

    public void Apply(object statusEffectTemplate, GameObject target, StatusApplicationContext context)
    {
        if (target == null || statusEffectTemplate == null)
        {
            return;
        }

        if (!target.TryGetComponent(out IStatusEffectHost host))
        {
            return;
        }

        Apply(statusEffectTemplate, host, context);
    }

    public void Apply(object statusEffectTemplate, IStatusEffectHost target, StatusApplicationContext context)
    {
        if (target == null || statusEffectTemplate == null)
        {
            return;
        }

        if (statusEffectTemplate is not StatusEffect statusEffect)
        {
            Debug.LogWarning("[StatusEffect] Apply called with unsupported template type.");
            return;
        }

        ApplyStatusEffect(statusEffect, target, context);
    }

    public void ApplyStatusEffect(StatusEffect statusEffect, GameObject target, GameObject caster) =>
        ApplyStatusEffect(statusEffect, target, StatusApplicationContext.FromCaster(caster, StatusApplySource.Unknown));

    public void ApplyStatusEffect(StatusEffect statusEffect, GameObject target, StatusApplicationContext context)
    {
        if (target == null || statusEffect == null)
        {
            return;
        }

        if (!target.TryGetComponent(out IStatusEffectHost host))
        {
            return;
        }

        ApplyStatusEffect(statusEffect, host, context);
    }

    public void ApplyStatusEffect(StatusEffect statusEffect, IStatusEffectHost target, StatusApplicationContext context)
    {
        if (target == null || statusEffect == null)
        {
            return;
        }

        GameObject caster = context.Caster;
        float delayTime = statusEffect.delayTime < 0
            ? statusEffect.tempDelayTime
            : statusEffect.delayTime + statusEffect.tempDelayTime;

        if (delayTime > 0)
        {
            TimerManager.Ins.WaitForTime(delayTime, () => StartStatusEffect(statusEffect, target, caster, context));
        }
        else
        {
            StartStatusEffect(statusEffect, target, caster, context);
        }
    }

    void StartStatusEffect(StatusEffect statusEffect, IStatusEffectHost target, GameObject caster, StatusApplicationContext context)
    {
        if (!target.IsStatusTargetAlive)
        {
            return;
        }

        if (statusEffect.triggerChance > 0f && UnityEngine.Random.value > statusEffect.triggerChance)
        {
            return;
        }

        if (statusEffect.canStack)
        {
            int currentStacks = GetStatusEffectCount(target, statusEffect.GetType());
            if (currentStacks >= statusEffect.maxStacks)
            {
                return;
            }
            if (statusEffect is ISingleInstanceStackable)
            {
                List<ActiveStatusEffect> existing = GetActiveStatusEffectsOfType(target, statusEffect.GetType());
                if (existing.Count > 0 && existing[0].statusEffect is ISingleInstanceStackable live)
                {
                    if (live.StackCount < live.MaxStacks)
                        live.PushStacks((int)context.Potency);
                    // raise StatusAppliedEvent so UI/Enemy can refresh
                    if (statusEffect.statusType == StatusEffectType.Shock)
                    {
                        TryTriggerShockOverloadMilestones(target, caster, context);
                    }
                    return;
                }
                // No existing instance → fall through and create normally (with _stackCount = 1)
            }
        }
        else if (GetStatusEffectCount(target, statusEffect.GetType()) > 0)
        {
            if (statusEffect is OverloadStatusEffect)
            {
                RefreshActiveStatusDuration(target, statusEffect.GetType(), statusEffect.duration);
                return;
            }

            return;
        }

        StatusEffect statusInstance = statusEffect.Clone();
        if (statusInstance == null)
        {
            Debug.LogWarning($"[StatusEffect] Clone returned null for {statusEffect.GetType().Name}");
            return;
        }

        if (context.Potency > 0f && statusInstance is not BurnStatusEffect)
        {
            statusInstance.baseDamage = context.Potency;
        }

        if (statusInstance.duration <= 0f)
        {
            statusInstance.OnApply(target, caster);
            EventBus<StatusAppliedEvent>.Raise(new StatusAppliedEvent
            {
                Host = target,
                Target = target.GetGameObject(),
                Caster = caster,
                Tag = statusInstance.statusType.ToTag(),
                Context = context,
            });
            synergyResolver?.OnStatusApplied(target, caster, statusInstance.statusType.ToTag(), context);
            return;
        }

        var activeStatus = new ActiveStatusEffect
        {
            statusEffect = statusInstance,
            target = target,
            caster = caster,
            startTime = Time.time,
            lastTickTime = Time.time,
        };

        if (!activeStatusEffects.ContainsKey(target))
        {
            activeStatusEffects[target] = new List<ActiveStatusEffect>();
        }

        activeStatusEffects[target].Add(activeStatus);
        statusInstance.OnApply(target, caster);

        EventBus<StatusAppliedEvent>.Raise(new StatusAppliedEvent
        {
            Host = target,
            Target = target.GetGameObject(),
            Caster = caster,
            Tag = statusInstance.statusType.ToTag(),
            Context = context,
        });

        synergyResolver?.OnStatusApplied(target, caster, statusInstance.statusType.ToTag(), context);
        if (statusEffect.statusType == StatusEffectType.Shock)
        {
            TryTriggerShockOverloadMilestones(target, caster, context);
        }

    }

    public IReadOnlyCollection<StatusEffectTag> GetActiveTags(GameObject target)
    {
        if (target == null || !target.TryGetComponent(out IStatusEffectHost host))
        {
            return Array.Empty<StatusEffectTag>();
        }

        return GetActiveTags(host);
    }

    public IReadOnlyCollection<StatusEffectTag> GetActiveTags(IStatusEffectHost target)
    {
        var tags = new HashSet<StatusEffectTag>();
        if (target == null || !activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
        {
            return tags;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            tags.Add(effects[i].statusEffect.statusType.ToTag());
        }

        return tags;
    }

    public int GetStatusEffectCount(GameObject target, Type statusType)
    {
        if (target == null || !target.TryGetComponent(out IStatusEffectHost host))
        {
            return 0;
        }

        return GetStatusEffectCount(host, statusType);
    }

    public int GetStatusEffectCount(IStatusEffectHost target, Type statusType)
    {
        if (target == null || !activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].statusEffect.GetType() == statusType)
            {
                if (effects[i].statusEffect is ISingleInstanceStackable s)
                {
                    count += s.StackCount;
                }
                else
                {
                    count++;
                }
            }
        }
        return count;
    }
    public List<ActiveStatusEffect> GetActiveStatusEffectsOfType(IStatusEffectHost target, Type statusType)
    {
        var matchingEffects = new List<ActiveStatusEffect>();
        if (target == null || !activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
        {
            return matchingEffects;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].statusEffect.GetType() == statusType)
            {
                matchingEffects.Add(effects[i]);
            }
        }

        return matchingEffects;
    }

    public bool RemoveStatusEffectById(IStatusEffectHost target, string effectId)
    {
        if (target == null || string.IsNullOrEmpty(effectId))
        {
            return false;
        }

        if (isUpdating)
        {
            QueueRemoveStatusEffectById(target, effectId);
            return true;
        }

        return RemoveStatusEffectByIdImmediate(target, effectId);
    }

    bool RemoveStatusEffectByIdImmediate(IStatusEffectHost target, string effectId)
    {
        if (target == null || string.IsNullOrEmpty(effectId) ||
            !activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
        {
            return false;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].effectId == effectId)
            {
                var removed = effects[i];
                removed.statusEffect.OnRemove(target, removed.caster);
                synergyResolver?.OnStatusRemoved(target, removed.statusEffect.statusType, removed.caster);
                effects.RemoveAt(i);
                if (effects.Count == 0)
                {
                    activeStatusEffects.Remove(target);
                    synergyCooldowns.Remove(target);
                    shockOverloadStates.Remove(target);
                }
                return true;
            }
        }

        return false;
    }

    public int RemoveAllStatusEffectsOfType(IStatusEffectHost target, Type statusType)
    {
        var effects = GetActiveStatusEffectsOfType(target, statusType);
        int removedCount = 0;
        for (int i = 0; i < effects.Count; i++)
        {
            if (RemoveStatusEffectById(target, effects[i].effectId))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    public void ClearAll(GameObject target)
    {
        if (target != null && target.TryGetComponent(out IStatusEffectHost host))
        {
            ClearAll(host);
        }
    }

    public void ClearAll(IStatusEffectHost target) => RemoveAllStatusEffectOfHost(target);

    public void RemoveAllStatusEffectOfGameObject(GameObject target) => ClearAll(target);

    public void RemoveAllStatusEffectOfHost(IStatusEffectHost target)
    {
        if (target == null)
        {
            return;
        }

        if (isUpdating)
        {
            QueueClearHost(target);
            return;
        }

        RemoveAllStatusEffectOfHostImmediate(target);
    }

    void RemoveAllStatusEffectOfHostImmediate(IStatusEffectHost target)
    {
        if (target == null || !activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
        {
            return;
        }

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var removed = effects[i];
            removed.statusEffect.OnRemove(target, removed.caster);
            synergyResolver?.OnStatusRemoved(target, removed.statusEffect.statusType, removed.caster);
            effects.RemoveAt(i);
        }

        activeStatusEffects.Remove(target);
        synergyCooldowns.Remove(target);
        shockOverloadStates.Remove(target);
    }

    public void RegisterReaction(ISynergyReaction reaction) => synergyResolver?.RegisterReaction(reaction);

    public void SetUnlockProvider(ISynergyUnlockProvider provider) => synergyResolver?.SetUnlockProvider(provider);

    public void SetSynergyRegistry(IReadOnlyList<SynergyDefinitionData> definitions) =>
        synergyResolver?.SetDefinitions(definitions);

    public void NotifyHostDefeated(IStatusEffectHost target, GameObject caster)
    {
        synergyResolver?.OnHostDefeated(target, caster);
    }

    static Type ToRuntimeType(StatusEffectType type) => type switch
    {
        StatusEffectType.Cold => typeof(ColdStatusEffect),
        StatusEffectType.Burn => typeof(BurnStatusEffect),
        StatusEffectType.Shock => typeof(ShockStatusEffect),
        StatusEffectType.Poison => typeof(PoisonStatusEffect),
        StatusEffectType.Bleed => typeof(BleedStatusEffect),
        StatusEffectType.Overload => typeof(OverloadStatusEffect),
        _ => null,
    };

    public int GetStackCount(IStatusEffectHost target, StatusEffectType type)
    {
        Type runtimeType = ToRuntimeType(type);
        if (runtimeType == null) return 0;
        List<ActiveStatusEffect> effects = GetActiveStatusEffectsOfType(target, runtimeType);
        if (effects.Count > 0 && effects[0].statusEffect is ISingleInstanceStackable s)
            return s.StackCount;          // ← read internal counter
        return effects.Count;
    }

    public int GetStackCount(GameObject target, StatusEffectType type)
    {
        if (target == null || !target.TryGetComponent(out IStatusEffectHost host))
        {
            return 0;
        }

        return GetStackCount(host, type);
    }

    public bool HasActiveTag(IStatusEffectHost target, StatusEffectTag tag)
    {
        IReadOnlyCollection<StatusEffectTag> tags = GetActiveTags(target);
        foreach (StatusEffectTag active in tags)
        {
            if (active == tag)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasActiveTag(GameObject target, StatusEffectTag tag)
    {
        if (target == null || !target.TryGetComponent(out IStatusEffectHost host))
        {
            return false;
        }

        return HasActiveTag(host, tag);
    }

    public int ExtendDuration(IStatusEffectHost target, StatusEffectType type, float deltaSeconds)
    {
        if (target == null || deltaSeconds <= 0f)
        {
            return 0;
        }

        Type runtimeType = ToRuntimeType(type);
        if (runtimeType == null)
        {
            return 0;
        }

        List<ActiveStatusEffect> effects = GetActiveStatusEffectsOfType(target, runtimeType);
        for (int i = 0; i < effects.Count; i++)
        {
            effects[i].statusEffect.duration += deltaSeconds;
        }

        return effects.Count;
    }

    public int ConsumeStacks(IStatusEffectHost target, StatusEffectType type, int count)
    {
        if (target == null || count <= 0)
        {
            return 0;
        }

        Type runtimeType = ToRuntimeType(type);
        if (runtimeType == null)
        {
            return 0;
        }

        List<ActiveStatusEffect> effects = GetActiveStatusEffectsOfType(target, runtimeType);
        // Single-instance path
        if (effects.Count > 0 && effects[0].statusEffect is ISingleInstanceStackable s)
        {
            int actual = s.PopStacks(count);
            if (s.StackCount <= 0)
                RemoveStatusEffectById(target, effects[0].effectId);  // remove the one instance
            return actual;
        }
        // Old multi-instance path (Burn, Shock, etc.)
        int toRemove = Mathf.Min(count, effects.Count);
        for (int i = 0; i < toRemove; i++)
        {
            RemoveStatusEffectById(target, effects[i].effectId);
        }

        return toRemove;
    }

    public float ComputeRemainingBurnDotDamage(IStatusEffectHost target)
    {
        if (target == null || !target.TryGetMaxHealth(out float maxHp) || maxHp <= 0f)
        {
            return 0f;
        }

        List<ActiveStatusEffect> burns = GetActiveStatusEffectsOfType(target, typeof(BurnStatusEffect));
        if (burns.Count == 0)
        {
            return 0f;
        }

        float tickDamage = maxHp * BurnStatusEffect.MaxHpDotPercentPerSecond;
        float total = 0f;
        float now = Time.time;
        for (int i = 0; i < burns.Count; i++)
        {
            ActiveStatusEffect active = burns[i];
            StatusEffect effect = active.statusEffect;
            if (effect == null)
            {
                continue;
            }

            float remainingSeconds = Mathf.Max(0f, effect.duration - (now - active.startTime));
            if (remainingSeconds <= 0f)
            {
                continue;
            }

            float tickInterval = effect.tickInterval > 0f ? effect.tickInterval : BurnStatusEffect.DefaultTickIntervalSeconds;
            total += remainingSeconds / tickInterval * tickDamage;
        }

        return total;
    }

    public float ComputePendingPoisonDamage(IStatusEffectHost target)
    {
        int stacks = GetStackCount(target, StatusEffectType.Poison); // now reads _stackCount
        float total = 0f;
        float remaining = stacks;
        while (remaining >= 1f)
        {
            total += remaining * PoisonStatusEffect.DamagePerStack;
            if (remaining <= 1f) break;
            remaining = Mathf.Floor(remaining * PoisonStatusEffect.DecayFraction);
        }
        return total;
    }

    public int ConsumeAllBurnStacks(IStatusEffectHost target)
    {
        int count = GetStackCount(target, StatusEffectType.Burn);
        return count > 0 ? ConsumeStacks(target, StatusEffectType.Burn, count) : 0;
    }

    public int MultiplyStacks(
        IStatusEffectHost target,
        StatusEffectType type,
        int factor,
        GameObject caster,
        StatusApplicationContext context)
    {
        if (target == null || factor <= 1)
        {
            return 0;
        }

        int current = GetStackCount(target, type);
        if (current <= 0)
        {
            return 0;
        }

        int toAdd = current * (factor - 1);
        Type runtimeType = ToRuntimeType(type);
        if (runtimeType == null)
        {
            return 0;
        }

        StatusEffect template = null;
        List<ActiveStatusEffect> existing = GetActiveStatusEffectsOfType(target, runtimeType);
        if (existing.Count > 0)
        {
            template = existing[0].statusEffect;
        }
        else
        {
            template = StatusEffectFactory.Create(type, 5f);
        }

        if (template == null)
        {
            return 0;
        }

        // ISingleInstanceStackable (e.g. Shock, Poison): one live instance holds all stacks.
        // Looping Apply would call PushStacks(context.Potency) per iteration — wrong amount and
        // wrong call count. Push the exact delta directly onto the live instance instead.
        if (existing.Count > 0 && existing[0].statusEffect is ISingleInstanceStackable liveStackable)
        {
            liveStackable.PushStacks(toAdd);
            return toAdd;
        }

        for (int i = 0; i < toAdd; i++)
        {
            StatusEffect clone = template.Clone();
            if (clone != null)
            {
                Apply(clone, target, context);
            }
        }

        return toAdd;
    }

    public bool TryGetSynergyCooldown(IStatusEffectHost target, string synergyId, out float lastTriggerTime)
    {
        lastTriggerTime = 0f;
        return synergyResolver != null && synergyResolver.TryGetCooldown(target, synergyId, out lastTriggerTime);
    }

    public void SetSynergyCooldown(IStatusEffectHost target, string synergyId, float time) =>
        synergyResolver?.SetCooldown(target, synergyId, time);

    void UpdateActiveStatusEffects()
    {
        if (activeStatusEffects.Count == 0)
        {
            return;
        }

        isUpdating = true;
        try
        {
            var targetsToRemove = new List<IStatusEffectHost>();
            var targetsSnapshot = new List<IStatusEffectHost>(activeStatusEffects.Keys);
            foreach (IStatusEffectHost target in targetsSnapshot)
            {
                if (!activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
                {
                    continue;
                }

                if (target == null)
                {
                    targetsToRemove.Add(target);
                    continue;
                }

                var aggregateTickedTypes = new HashSet<StatusEffectType>();
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (i >= effects.Count)
                        {
                            continue;
                        }

                        ActiveStatusEffect current = effects[i];
                        if (UpdateActiveStatusEffect(current, aggregateTickedTypes))
                        {
                            if (!activeStatusEffects.TryGetValue(target, out effects))
                            {
                                break;
                            }

                            int removeIndex = FindEffectIndex(effects, current);
                            if (removeIndex >= 0)
                            {
                                effects.RemoveAt(removeIndex);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"{e} === INDEX = {i}");
                    }
                }

                if (activeStatusEffects.TryGetValue(target, out effects) && effects.Count == 0)
                {
                    targetsToRemove.Add(target);
                }
            }

            for (int i = 0; i < targetsToRemove.Count; i++)
            {
                activeStatusEffects.Remove(targetsToRemove[i]);
                shockOverloadStates.Remove(targetsToRemove[i]);
            }
        }
        finally
        {
            isUpdating = false;
            FlushDeferredRemovals();
        }
    }

    int FindEffectIndex(List<ActiveStatusEffect> effects, ActiveStatusEffect targetEffect)
    {
        if (effects == null || targetEffect == null)
        {
            return -1;
        }

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(effects[i], targetEffect) || effects[i].effectId == targetEffect.effectId)
            {
                return i;
            }
        }

        return -1;
    }

    void QueueRemoveStatusEffectById(IStatusEffectHost target, string effectId)
    {
        if (target == null || string.IsNullOrEmpty(effectId))
        {
            return;
        }

        if (deferredClearHosts.Contains(target))
        {
            return;
        }

        if (!deferredRemoveById.TryGetValue(target, out HashSet<string> ids))
        {
            ids = new HashSet<string>();
            deferredRemoveById[target] = ids;
        }

        ids.Add(effectId);
    }

    void QueueClearHost(IStatusEffectHost target)
    {
        if (target == null)
        {
            return;
        }

        deferredClearHosts.Add(target);
        deferredRemoveById.Remove(target);
    }

    void FlushDeferredRemovals()
    {
        if (deferredClearHosts.Count == 0 && deferredRemoveById.Count == 0)
        {
            return;
        }

        if (deferredClearHosts.Count > 0)
        {
            var clearTargets = new List<IStatusEffectHost>(deferredClearHosts);
            deferredClearHosts.Clear();
            for (int i = 0; i < clearTargets.Count; i++)
            {
                RemoveAllStatusEffectOfHostImmediate(clearTargets[i]);
            }
        }

        if (deferredRemoveById.Count > 0)
        {
            var deferredEntries = new List<KeyValuePair<IStatusEffectHost, HashSet<string>>>(deferredRemoveById);
            deferredRemoveById.Clear();
            for (int i = 0; i < deferredEntries.Count; i++)
            {
                IStatusEffectHost target = deferredEntries[i].Key;
                HashSet<string> ids = deferredEntries[i].Value;
                if (ids == null)
                {
                    continue;
                }

                foreach (string id in ids)
                {
                    RemoveStatusEffectByIdImmediate(target, id);
                }
            }
        }
    }

    void RefreshActiveStatusDuration(IStatusEffectHost target, Type statusType, float durationSeconds)
    {
        if (target == null || statusType == null || durationSeconds <= 0f)
        {
            return;
        }

        List<ActiveStatusEffect> effects = GetActiveStatusEffectsOfType(target, statusType);
        if (effects.Count == 0)
        {
            return;
        }

        float now = Time.time;
        for (int i = 0; i < effects.Count; i++)
        {
            effects[i].statusEffect.duration = durationSeconds;
            effects[i].startTime = now;
            effects[i].lastTickTime = now;
        }
    }

    void TryTriggerShockOverloadMilestones(
        IStatusEffectHost target,
        GameObject caster,
        StatusApplicationContext context)
    {
        if (target == null)
        {
            return;
        }

        if (!shockOverloadStates.TryGetValue(target, out ShockOverloadState state))
        {
            state = new ShockOverloadState { NextThreshold = shockFirstOverloadThreshold };
            shockOverloadStates[target] = state;
        }

        int shockCount = GetStackCount(target, StatusEffectType.Shock);
        bool triggeredAny = false;
        while (shockCount >= state.NextThreshold)
        {
            triggeredAny = true;
            state.HighestTriggeredThreshold = state.NextThreshold;
            ApplyOrRefreshOverload(target, caster, context);
            state.NextThreshold = ComputeNextShockThreshold(state);
            shockCount = GetStackCount(target, StatusEffectType.Shock);
        }

        if (triggeredAny)
        {
            int remainingShock = GetStackCount(target, StatusEffectType.Shock);
            if (remainingShock > 0)
            {
                ConsumeStacks(target, StatusEffectType.Shock, remainingShock);
            }
        }
    }

    int ComputeNextShockThreshold(ShockOverloadState state)
    {
        int previous = Mathf.Max(1, state.HighestTriggeredThreshold);
        float scaled = previous * shockOverloadThresholdMultiplier;
        return Mathf.Max(previous + 1, Mathf.RoundToInt(scaled));
    }

    void ApplyOrRefreshOverload(IStatusEffectHost target, GameObject caster, StatusApplicationContext context)
    {
        float duration = shockOverloadDuration;
        List<ActiveStatusEffect> existing = GetActiveStatusEffectsOfType(target, typeof(OverloadStatusEffect));
        if (existing.Count > 0)
        {
            RefreshActiveStatusDuration(target, typeof(OverloadStatusEffect), duration);
            return;
        }

        StatusEffect overload = StatusEffectFactory.Create(StatusEffectType.Overload, duration);
        if (overload == null)
        {
            return;
        }

        var overloadContext = context;
        overloadContext.Source = StatusApplySource.Synergy;
        ApplyStatusEffect(overload, target, overloadContext);
    }

    void SyncAggregateLastTickTime(IStatusEffectHost target, StatusEffectType type, float tickTime)
    {
        if (target == null || !activeStatusEffects.TryGetValue(target, out List<ActiveStatusEffect> effects))
        {
            return;
        }

        Type runtimeType = ToRuntimeType(type);
        if (runtimeType == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].statusEffect != null && effects[i].statusEffect.GetType() == runtimeType)
            {
                effects[i].lastTickTime = tickTime;
            }
        }
    }

    bool UpdateActiveStatusEffect(ActiveStatusEffect activeStatus, HashSet<StatusEffectType> aggregateTickedTypes)
    {
        float currentTime = Time.time;
        float interval = activeStatus.statusEffect.tickInterval;
        if (activeStatus.target == null)
        {
            return true;
        }

        if (interval > 0f && currentTime - activeStatus.lastTickTime >= interval)
        {
            if (activeStatus.statusEffect is IAggregateStatusTickEffect aggregateEffect)
            {
                StatusEffectType type = activeStatus.statusEffect.statusType;
                if (aggregateTickedTypes.Add(type))
                {
                    aggregateEffect.OnAggregateTick(activeStatus.target, activeStatus.caster);
                    SyncAggregateLastTickTime(activeStatus.target, type, currentTime);
                }
            }
            else
            {
                activeStatus.statusEffect.OnTick(activeStatus.target, activeStatus.caster);
                activeStatus.lastTickTime = currentTime;
            }
        }

        if (currentTime - activeStatus.startTime >= activeStatus.statusEffect.duration)
        {
            activeStatus.statusEffect.OnRemove(activeStatus.target, activeStatus.caster);
            synergyResolver?.OnStatusRemoved(activeStatus.target, activeStatus.statusEffect.statusType, activeStatus.caster);
            return true;
        }

        if (!activeStatus.statusEffect.IsTargetAvailable)
        {
            activeStatus.statusEffect.OnRemove(activeStatus.target, activeStatus.caster);
            synergyResolver?.OnStatusRemoved(activeStatus.target, activeStatus.statusEffect.statusType, activeStatus.caster);
            return true;
        }

        return false;
    }

    internal float ComputeNextShockThreshold(IStatusEffectHost target)
    {
        if (target == null || !shockOverloadStates.TryGetValue(target, out ShockOverloadState state))
        {
            return 100f;
        }

        return state.NextThreshold;
    }

    /// <summary>
    /// Clean up all stat modifiers applied via synergy reactions on a host.
    /// Called when a character dies or is removed from the scene to prevent modifier leaks.
    /// </summary>
    public void CleanupStatModifiers(IStatusEffectHost host)
    {
        if (host == null)
        {
            return;
        }

        // If the host implements IModStat, remove any synergy-sourced modifiers.
        // This is a hook point for cleanup of persistent stat modifiers applied by synergy reactions.
        var modStatHost = host as IModStat;
        if (modStatHost != null)
        {
            // Future implementation: iterate through all synergy-sourced modifiers and remove them.
            // For now, this serves as the documented cleanup interface.
            // The actual cleanup can be expanded to track and remove individual modifiers by source ID.
        }
    }
}

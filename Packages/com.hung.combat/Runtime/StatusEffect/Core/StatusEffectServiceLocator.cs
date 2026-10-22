using Hung.Base;
using Hung.Combat;
using Hung.StatusEffect;
using UnityEngine;

public static class StatusEffectServiceLocator
{
    private static IStatusEffectService overrideService;
    public static IStatusEffectService Service
    {
        get => overrideService ?? StatusEffectManager.Ins;
        set => overrideService = value;
    }

    public static void Apply(StatusEffect effect, GameObject target, StatusApplicationContext context)
    {
        if (Service == null)
        {
            return;
        }

        Service.Apply(effect, target, context);
    }

    public static void Apply(StatusEffect effect, GameObject target, GameObject caster) =>
        Apply(effect, target, StatusApplicationContext.FromCaster(caster, StatusApplySource.Unknown));

    public static int GetCount(GameObject target, System.Type statusType) =>
        Service != null ? Service.GetStatusEffectCount(target, statusType) : 0;

    public static int GetStackCount(IStatusEffectHost target, StatusEffectType type) =>
        Manager != null ? Manager.GetStackCount(target, type) : 0;

    public static bool HasActiveTag(IStatusEffectHost target, StatusEffectTag tag) =>
        Manager != null && Manager.HasActiveTag(target, tag);

    public static int ExtendDuration(IStatusEffectHost target, StatusEffectType type, float deltaSeconds) =>
        Manager != null ? Manager.ExtendDuration(target, type, deltaSeconds) : 0;

    public static int ConsumeStacks(IStatusEffectHost target, StatusEffectType type, int count) =>
        Manager != null ? Manager.ConsumeStacks(target, type, count) : 0;

    public static float ComputeRemainingBurnDotDamage(IStatusEffectHost target) =>
        Manager != null ? Manager.ComputeRemainingBurnDotDamage(target) : 0f;

    public static float ComputePendingPoisonDamage(IStatusEffectHost target) =>
        Manager != null ? Manager.ComputePendingPoisonDamage(target) : 0f;
    public static float ComputeNextShockThreshold(IStatusEffectHost target) =>
        Manager != null ? Manager.ComputeNextShockThreshold(target) : 0f;
    public static int ConsumeAllBurnStacks(IStatusEffectHost target) =>
        Manager != null ? Manager.ConsumeAllBurnStacks(target) : 0;

    public static int MultiplyStacks(
        IStatusEffectHost target,
        StatusEffectType type,
        int factor,
        GameObject caster,
        StatusApplicationContext context) =>
        Manager != null ? Manager.MultiplyStacks(target, type, factor, caster, context) : 0;

    public static int RemoveAllOfType(IStatusEffectHost target, System.Type statusType) =>
        Manager != null ? Manager.RemoveAllStatusEffectsOfType(target, statusType) : 0;

    public static void NotifyHostDefeated(IStatusEffectHost target, GameObject caster) =>
        Manager?.NotifyHostDefeated(target, caster);

    static StatusEffectManager Manager =>
        Service as StatusEffectManager ?? StatusEffectManager.Ins;
}

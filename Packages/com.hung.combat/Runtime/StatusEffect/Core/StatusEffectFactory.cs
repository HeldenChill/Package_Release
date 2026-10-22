public static class StatusEffectFactory
{
    public const int INFINITY = 99999;
    public static StatusEffect Create(StatusEffectType type, float duration)
    {
        switch (type)
        {
            case StatusEffectType.Cold:
                return new ColdStatusEffect
                {
                    statusName = "Cold",
                    duration = duration,
                    tickInterval = 0f,
                    canStack = true,
                    maxStacks = 15,
                    statusType = StatusEffectType.Cold,
                };
            case StatusEffectType.Burn:
                return BurnStatusEffect.CreateTemplate(duration);
            case StatusEffectType.Shock:
                return new ShockStatusEffect
                {
                    statusName = "Shock",
                    duration = INFINITY,
                    tickInterval = 0f,
                    canStack = true,
                    maxStacks = INFINITY,
                    statusType = StatusEffectType.Shock,
                };
            case StatusEffectType.Overload:
                return new OverloadStatusEffect
                {
                    statusName = "Overload",
                    duration = duration > 0f ? duration : 1f,
                    tickInterval = 0f,
                    canStack = false,
                    maxStacks = 1,
                    statusType = StatusEffectType.Overload,
                };
            case StatusEffectType.Bleed:
                return new BleedStatusEffect
                {
                    statusName = "Bleed",
                    duration = duration,
                    tickInterval = 0f,
                    canStack = true,
                    maxStacks = BleedStatusEffect.MaxStacks,
                    statusType = StatusEffectType.Bleed,
                };
            case StatusEffectType.Poison:
                return new PoisonStatusEffect
                {
                    statusName = "Poison",
                    duration = INFINITY,
                    tickInterval = PoisonStatusEffect.TickIntervalSeconds,
                    canStack = true,
                    maxStacks = INFINITY,
                    statusType = StatusEffectType.Poison,
                };
            default:
                return null;
        }
    }
}

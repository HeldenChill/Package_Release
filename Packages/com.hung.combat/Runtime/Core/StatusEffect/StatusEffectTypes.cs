public enum DamageType
{
    None = -1,
    Flat = 0,
    AdditionalDamagePercent = 1,
}

public enum StatusEffectType
{
    None = -1,
    Cold = 0,
    Burn = 1,
    Shock = 2,
    Poison = 3,
    Bleed = 4,
    Overload = 5,
    Mud = 6,
    Electric = 7,
    Arcane = 8,
}

public static class StatusEffectTypeExtensions
{
    public static Hung.StatusEffect.StatusEffectTag ToTag(this StatusEffectType type) =>
        (Hung.StatusEffect.StatusEffectTag)(int)type;
}

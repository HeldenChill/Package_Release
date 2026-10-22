using Hung.Base;

namespace Hung.Combat
{
    /// <summary>
    /// Runtime enemy damage-taken multiplier mutations (EnemyStats.*Multipiliers).
    /// </summary>
    public interface IDamageTakenMultiplierReceiver
    {
        void AddDamageTakenMultiplier(ELEMENTAL type, float flatBonus, object source);
        void RemoveDamageTakenMultiplierFromSource(ELEMENTAL type, object source);
        void AddDamageTakenMultiplierAllTypes(float flatBonus, object source);
        void RemoveDamageTakenMultiplierFromSourceAllTypes(object source);
        void AddDamageTakenPercentAdd(ELEMENTAL type, float percentAdd, object source);
        void AddDamageTakenPercentAddAllTypes(float percentAdd, object source);
        void RemoveDamageTakenPercentAddFromSource(ELEMENTAL type, object source);
        void RemoveDamageTakenPercentAddFromSourceAllTypes(object source);
    }
}

using System.Collections.Generic;
using Hung.Base;

namespace Hung.Combat
{
    /// <summary>
    /// Calculation-time profile wrapper. Prefer <see cref="IDamageTakenMultiplierReceiver"/> on enemies for persistent vulnerability.
    /// </summary>
    public sealed class CompositeResistanceProfile : IElementalResistanceProfile
    {
        readonly IElementalResistanceProfile inner;
        readonly Dictionary<ELEMENTAL, float> additiveBonuses = new();

        public CompositeResistanceProfile(IElementalResistanceProfile inner)
        {
            this.inner = inner;
        }

        public void AddBonus(ELEMENTAL type, float additiveMultiplier)
        {
            if (!additiveBonuses.ContainsKey(type))
            {
                additiveBonuses[type] = 0f;
            }

            additiveBonuses[type] += additiveMultiplier;
        }

        public float GetMultiplier(ELEMENTAL type)
        {
            float baseMult = inner != null ? inner.GetMultiplier(type) : 1f;
            return additiveBonuses.TryGetValue(type, out float bonus) ? baseMult + bonus : baseMult;
        }
    }
}

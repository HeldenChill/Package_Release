using Hung.Combat;
using UnityEngine;

namespace Hung.Combat
{
    /// <summary>
    /// Status effects that tick once per host/type per interval (e.g. Poison aggregate tick).
    /// Per-instance ticking (Burn) does not implement this interface.
    /// </summary>
    public interface IAggregateStatusTickEffect
    {
        void OnAggregateTick(IStatusEffectHost target, GameObject caster);
    }
}

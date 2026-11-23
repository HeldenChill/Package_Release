using System.Collections.Generic;
using Hung.Combat;
using UnityEngine;

namespace Hung.StatusEffect
{
    public interface IStatusEffectService
    {
        void Apply(object statusEffectTemplate, GameObject target, GameObject caster);
        void Apply(object statusEffectTemplate, GameObject target, StatusApplicationContext context);
        void Apply(object statusEffectTemplate, IStatusEffectHost target, StatusApplicationContext context);
        IReadOnlyCollection<StatusEffectTag> GetActiveTags(GameObject target);
        IReadOnlyCollection<StatusEffectTag> GetActiveTags(IStatusEffectHost target);
        int GetStatusEffectCount(GameObject target, System.Type statusType);
        int GetStatusEffectCount(IStatusEffectHost target, System.Type statusType);
        /// <summary>Removes every active effect on <paramref name="target"/>. Call it when a host dies or
        /// returns to a pool; a destroyed host is dropped automatically, a pooled one is not.</summary>
        void ClearAll(GameObject target);
        /// <inheritdoc cref="ClearAll(GameObject)"/>
        void ClearAll(IStatusEffectHost target);
        void RegisterReaction(ISynergyReaction reaction);
        void SetUnlockProvider(ISynergyUnlockProvider provider);
        void SetSynergyRegistry(IReadOnlyList<SynergyDefinitionData> definitions);
        void NotifyHostDefeated(IStatusEffectHost target, GameObject caster);
    }
}

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
        void ClearAll(GameObject target);
        void ClearAll(IStatusEffectHost target);
        void RegisterReaction(ISynergyReaction reaction);
        void SetUnlockProvider(ISynergyUnlockProvider provider);
        void SetSynergyRegistry(IReadOnlyList<SynergyDefinitionData> definitions);
        void NotifyHostDefeated(IStatusEffectHost target, GameObject caster);
    }
}

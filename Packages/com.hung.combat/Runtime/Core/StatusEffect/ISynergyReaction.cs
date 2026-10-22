using Hung.Combat;
using UnityEngine;

namespace Hung.StatusEffect
{
    public interface ISynergyReaction
    {
        string ReactionId { get; }
        void Execute(SynergyDefinitionData definition, IStatusEffectHost target, GameObject caster, StatusApplicationContext context, StatusEffectTag triggeringType);

        /// <summary>
        /// Called when a status effect is removed from a host.
        /// Default implementation is a no-op so existing reactions are unaffected.
        /// </summary>
        void OnStatusRemoved(IStatusEffectHost target, StatusEffectType removedType, GameObject caster) { }
    }

    public struct SynergyDefinitionData
    {
        public string SynergyId;
        public StatusEffectTag[] RequiredTags;
        public string UnlockKey;
        public SynergyUnlockScope UnlockScope;
        public string ReactionId;
        public float[] Parameters;
        public float CooldownPerTarget;
        public SynergyTriggerType TriggerType;
    }
}

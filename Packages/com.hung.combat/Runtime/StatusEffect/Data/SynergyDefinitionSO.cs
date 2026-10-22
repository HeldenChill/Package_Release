using Hung.StatusEffect;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "SynergyDefinition", menuName = "ScriptableObjects/Gameplay/SynergyDefinition", order = 2)]
public class SynergyDefinitionSO : ScriptableObject
{
    public string SynergyId;
    public StatusEffectTag[] RequiredTags;
    public SynergyTriggerType TriggerType = SynergyTriggerType.OnStatusApplied;
    public SynergyUnlockScope UnlockScope = SynergyUnlockScope.CasterScoped;
    public string UnlockKey;
    public string ReactionId;
    public float[] Parameters;
    public float CooldownPerTarget;

    public SynergyDefinitionData ToData() => new SynergyDefinitionData
    {
        SynergyId = SynergyId,
        RequiredTags = RequiredTags,
        UnlockKey = UnlockKey,
        UnlockScope = UnlockScope,
        ReactionId = ReactionId,
        Parameters = Parameters,
        CooldownPerTarget = CooldownPerTarget,
        TriggerType = TriggerType,

    };
}

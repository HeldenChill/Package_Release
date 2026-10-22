using System.Collections.Generic;
using Hung.StatusEffect;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectModuleConfig", menuName = "ScriptableObjects/Gameplay/StatusEffectModuleConfig", order = 2)]
public class StatusEffectModuleConfigSO : ScriptableObject
{
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<SynergyDefinitionSO> Synergies = new();

    [Title("Shock / Overload")]
    public int ShockFirstOverloadThreshold = 100;
    public float ShockOverloadThresholdMultiplier = 2f;
    public float ShockOverloadDuration = 1f;

    [Title("Poison")]
    public float PoisonTickInterval = 2f;
    public float PoisonDamagePerStack = 1f;
    [Range(0f, 1f)]
    public float PoisonDecayFraction = 0.5f;

    [Title("Bleed")]
    public float BleedDamageTakenPerStack = 0.05f;
    public int BleedMaxStacks = 30;

    public List<SynergyDefinitionData> BuildDefinitionList()
    {
        var list = new List<SynergyDefinitionData>();
        for (int i = 0; i < Synergies.Count; i++)
        {
            if (Synergies[i] != null)
            {
                list.Add(Synergies[i].ToData());
            }
        }

        return list;
    }
}

using Hung.Base;
using Hung.StatusEffect;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StatusEffectManager))]
public class StatusEffectModule : MonoBehaviour
{
    public static StatusEffectModule Instance { get; private set; }

    [SerializeField] StatusEffectManager manager;
    [SerializeField] StatusEffectModuleConfigSO config;

    SynergyResolver synergyResolver;

    void Awake()
    {
        Instance = this;
        if (manager == null)
        {
            manager = GetComponent<StatusEffectManager>();
        }

        synergyResolver = new SynergyResolver(manager);
        manager.ConfigureSynergy(synergyResolver);
        manager.Configure(config);
        StatusEffectServiceLocator.Service = manager;

        var definitions = config != null ? config.BuildDefinitionList() : new List<SynergyDefinitionData>();
        AppendDefaultSynergies(definitions);
        manager.SetSynergyRegistry(definitions);
    }

    static void AppendDefaultSynergies(List<SynergyDefinitionData> definitions)
    {
        // definitions.Add(new SynergyDefinitionData
        // {
        //     SynergyId = "burn_electric_extend",
        //     RequiredTags = new[] { StatusEffectTag.Burn, StatusEffectTag.Shock },
        //     UnlockKey = string.Empty,
        //     UnlockScope = SynergyUnlockScope.RunWide,
        //     ReactionId = "BurnElectric_Extend",
        //     Parameters = new[] { 2f },
        // });

        // definitions.Add(new SynergyDefinitionData
        // {
        //     SynergyId = "burn_spread_on_death",
        //     RequiredTags = new[] { StatusEffectTag.Burn },
        //     TriggerType = SynergyTriggerType.OnHostDefeated,
        //     UnlockKey = string.Empty,
        //     UnlockScope = SynergyUnlockScope.RunWide,
        //     ReactionId = "BurnSpread_OnDeath",
        //     Parameters = new[] { 3f, 5f },
        //     CooldownPerTarget = 0f,
        // });

        // definitions.Add(new SynergyDefinitionData
        // {
        //     SynergyId = "cold_shock_crystalline_overload",
        //     RequiredTags = new[] { StatusEffectTag.Cold, StatusEffectTag.Shock },
        //     TriggerType = SynergyTriggerType.OnStatusApplied,
        //     UnlockKey = string.Empty,
        //     UnlockScope = SynergyUnlockScope.RunWide,
        //     ReactionId = "ColdShock_CrystallineOverload",
        //     Parameters = new[] { 3f },
        //     CooldownPerTarget = 0f,
        // });

        // definitions.Add(new SynergyDefinitionData
        // {
        //     SynergyId = "resonant_poison",
        //     RequiredTags = new[] { StatusEffectTag.Poison },
        //     TriggerType = SynergyTriggerType.OnStatusApplied,
        //     UnlockKey = string.Empty,
        //     UnlockScope = SynergyUnlockScope.RunWide,
        //     ReactionId = "ResonantPoison",
        //     Parameters = new float[] { },
        //     CooldownPerTarget = 0f,
        // });

        // definitions.Add(new SynergyDefinitionData
        // {
        //     SynergyId = "poison_spread_burst",
        //     RequiredTags = new[] { StatusEffectTag.Poison, StatusEffectTag.Burn },
        //     TriggerType = SynergyTriggerType.OnStatusApplied,
        //     UnlockKey = string.Empty,
        //     UnlockScope = SynergyUnlockScope.RunWide,
        //     ReactionId = "PoisonSpread_Burst",
        //     Parameters = new[] { 2f },
        //     CooldownPerTarget = 0f,
        // });

        // definitions.Add(new SynergyDefinitionData
        // {
        //     SynergyId = "cold_spread_on_death",
        //     RequiredTags = new[] { StatusEffectTag.Cold },
        //     TriggerType = SynergyTriggerType.OnHostDefeated,
        //     UnlockKey = string.Empty,
        //     UnlockScope = SynergyUnlockScope.RunWide,
        //     ReactionId = "ColdSpread_OnDeath",
        //     Parameters = new[] { 4f, 3f, 10f, 10f },
        //     CooldownPerTarget = 0f,
        // });
    }

    void OnDestroy()
    {
        if (ReferenceEquals(StatusEffectServiceLocator.Service, manager))
        {
            StatusEffectServiceLocator.Service = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetUnlockProvider(ISynergyUnlockProvider provider) => manager.SetUnlockProvider(provider);

    public void RefreshSynergyDefinitions()
    {
        var definitions = config != null ? config.BuildDefinitionList() : new List<SynergyDefinitionData>();
        AppendDefaultSynergies(definitions);
        manager.SetSynergyRegistry(definitions);
    }
}

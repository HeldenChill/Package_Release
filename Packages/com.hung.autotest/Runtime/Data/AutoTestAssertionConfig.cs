using System;
using System.Collections.Generic;

namespace Hung.AutoTest
{
    public enum AutoTestAssertionType
    {
        NoExceptionLog,
        ScenarioStarted,
        WaveStarted,
        EnemySpawned,
        DamageStatisticsHasEvents,
        CombatDamageProgress,
        WaveNotStuck,
        ScenarioTimeout,
        NoNaNTransform,
        NoInvalidEnemyState,
        StatusEffectActive,
        StatusStackBehavior,
        SynergyTriggered,
        SynergyNotTriggered,
        PetStatValue,
        PetStatDeltaFromBaseline,
        SupportAffectsPet,
        PetTargetAcquired,
        PetAttacking,
        PetDetectionZoneInitialized,
        EnemyKilledCount,
        AllEnemiesCleared,
        WaveCompleted,
        EnemyCountConsistent,
        EnemySlowed,
        EnemyOverloadTriggered,
        TotalDamageAtLeast,
        GameplayPhaseReached,
        SupportAlive,
        // Reserved serialized extension slots. Host games define their semantic labels in glue.
        Extension29 = 29,
        Extension30 = 30,
        Extension31 = 31,
        Extension32 = 32,
        Extension33 = 33,
        Extension34 = 34,
        Extension35 = 35,
        Extension36 = 36,
        Extension37 = 37,
        Extension38 = 38,
        Extension39 = 39,
        Extension40 = 40,
        Extension41 = 41,
        Extension42 = 42
    }

    public enum AutoTestAssertionSeverity
    {
        Info,
        Warning,
        Failure,
        Fatal
    }

    [Serializable]
    public sealed class AutoTestAssertionConfig
    {
        public bool enabled = true;
        public AutoTestAssertionType type;
        public string assertionId;
        public AutoTestAssertionSeverity severity = AutoTestAssertionSeverity.Failure;
        public float timeoutSeconds = 10f;
        public float threshold = 0f;
        public string stringParam;
        public string stringParam2;
        public int intParam;
        public float tolerance = 0.001f;

        public static AutoTestAssertionConfig Create(AutoTestAssertionType type, AutoTestAssertionSeverity severity = AutoTestAssertionSeverity.Failure, float timeoutSeconds = 10f, float threshold = 0f)
        {
            return new AutoTestAssertionConfig
            {
                enabled = true,
                type = type,
                severity = severity,
                timeoutSeconds = timeoutSeconds,
                threshold = threshold,
                tolerance = 0.001f
            };
        }

        public static AutoTestAssertionConfig Create(
            string assertionId,
            AutoTestAssertionSeverity severity = AutoTestAssertionSeverity.Failure,
            float timeoutSeconds = 10f,
            float threshold = 0f)
        {
            return new AutoTestAssertionConfig
            {
                enabled = true,
                assertionId = assertionId,
                severity = severity,
                timeoutSeconds = timeoutSeconds,
                threshold = threshold,
                tolerance = 0.001f
            };
        }

        public static List<AutoTestAssertionConfig> CreateDefaultList()
        {
            return new List<AutoTestAssertionConfig>
            {
                Create(AutoTestAssertionType.NoExceptionLog, AutoTestAssertionSeverity.Fatal),
                Create(AutoTestAssertionType.ScenarioStarted, AutoTestAssertionSeverity.Fatal, 3f),
                Create(AutoTestAssertionType.WaveStarted, AutoTestAssertionSeverity.Failure, 8f),
                Create(AutoTestAssertionType.EnemySpawned, AutoTestAssertionSeverity.Failure, 10f),
                Create(AutoTestAssertionType.DamageStatisticsHasEvents, AutoTestAssertionSeverity.Failure, 12f),
                Create(AutoTestAssertionType.CombatDamageProgress, AutoTestAssertionSeverity.Failure, 12f),
                Create(AutoTestAssertionType.WaveNotStuck, AutoTestAssertionSeverity.Failure, 10f),
                Create(AutoTestAssertionType.ScenarioTimeout, AutoTestAssertionSeverity.Failure),
                Create(AutoTestAssertionType.NoNaNTransform, AutoTestAssertionSeverity.Fatal),
                Create(AutoTestAssertionType.NoInvalidEnemyState, AutoTestAssertionSeverity.Failure),
                Create(AutoTestAssertionType.PetDetectionZoneInitialized, AutoTestAssertionSeverity.Failure, 5f),
                Create(AutoTestAssertionType.EnemyCountConsistent, AutoTestAssertionSeverity.Failure),
                Create(AutoTestAssertionType.SupportAlive, AutoTestAssertionSeverity.Warning)
            };
        }
    }
}

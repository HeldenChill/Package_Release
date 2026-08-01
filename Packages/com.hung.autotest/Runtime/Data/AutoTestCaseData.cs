using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hung.AutoTest
{
    [CreateAssetMenu(fileName = "AutoTestCase", menuName = "Tools/Auto Test/Auto Test Case")]
    public sealed class AutoTestCaseData : ScriptableObject
    {
        [ReadOnly] public string testId = "pvm_case";
        [ReadOnly] public string displayName = "Auto Test Case";
        [TextArea(2, 5)] public string description;

        [Header("Scenario")]
        // ScriptableObject, not TestScenarioData: keeps the AutoTest core decoupled from
        // the game's scenario module. The game's IAutoTestScenarioExecutor casts it.
        // Serialized asset references survive (same field name, refs are GUID+fileID).
        public ScriptableObject scenario;
        public bool runScenario = true;
        public bool startWaveIfScenarioDoesNot = true;
        public float delayAfterScenarioRunSeconds = 0.25f;
        public float delayBeforeStartWaveSeconds = 0.25f;

        [FormerlySerializedAs("heroSpellIndexOverride")]
        public int extensionInt = -1;

        [Header("Damage Statistics")]
        public bool enableDamageStatistics = true;
        public bool resetDamageStatisticsBeforeRun = true;

        [Header("Runtime")]
        public float timeoutSeconds = 30f;
        public float snapshotIntervalSeconds = 0.25f;
        public bool failFast = false;

        [Header("Assertions")]
        public List<AutoTestAssertionConfig> assertions = AutoTestAssertionConfig.CreateDefaultList();

        [Header("Pass Condition")]
        public AutoTestPassConditionConfig passCondition = AutoTestPassConditionConfig.DurationSurvived(15f);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (testId != name || displayName != name)
            {
                testId = name;
                displayName = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}

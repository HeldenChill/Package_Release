using System;

namespace Hung.AutoTest
{
    public enum AutoTestPassConditionType
    {
        DurationSurvived,
        WaveCompleted,
        EnemyKilledCountReached,
        DamageReached,
        NoFatalFailureUntilTimeout
    }

    [Serializable]
    public sealed class AutoTestPassConditionConfig
    {
        public AutoTestPassConditionType type = AutoTestPassConditionType.DurationSurvived;
        public float requiredDurationSeconds = 15f;
        public int requiredWaveCompletedCount = 1;
        public int requiredEnemyKilledCount = 1;
        public float requiredTotalDamage = 1f;

        public static AutoTestPassConditionConfig DurationSurvived(float seconds)
        {
            return new AutoTestPassConditionConfig
            {
                type = AutoTestPassConditionType.DurationSurvived,
                requiredDurationSeconds = seconds
            };
        }
    }
}

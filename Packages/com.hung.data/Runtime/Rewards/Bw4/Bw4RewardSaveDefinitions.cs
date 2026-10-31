using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
namespace Hung.Data.Rewards
{
    public static class Bw4RewardSaveDefinitions
    {
        // Same canonical keys/aliases, selected explicit schema 2. Never compose alongside schema-1 legacy owners.
        public static SaveDefinition<DailyRewardSaveData> DailyReward(ISaveCodec codec, ISaveProtector protector)
            => Make<DailyRewardSaveData>("daily-reward", nameof(DailyRewardSaveData), codec, protector, v => v != null
                && v.currentProgress >= 0 && v.rewardDayKey >= 0 && v.dayOfYear >= 0 && v.lastFreeClaimTime >= 0
                && v.lastFreeClaimUtcTicks >= 0 && (v.bw4Pending == null || v.bw4Pending.Valid()));
        public static SaveDefinition<SpinWheelSaveData> SpinWheel(ISaveCodec codec, ISaveProtector protector)
            => Make<SpinWheelSaveData>("spin-wheel", nameof(SpinWheelSaveData), codec, protector, v => v != null
                && v.adsSpinToday >= 0 && v.rewardDayKey >= 0 && v.dayOfYear >= 0 && v.spinOrdinal >= 0
                && (v.bw4Pending == null || v.bw4Pending.Valid()));
        private static SaveDefinition<T> Make<T>(string key, string alias, ISaveCodec codec, ISaveProtector protector, Func<T, bool> valid) where T : new()
            => new SaveDefinition<T>(key, 2, () => new T(),
                v => valid(v) ? SaveValidationResult.Valid() : SaveValidationResult.Invalid("BW4_STATE_INVALID"),
                new ISaveMigration[] { new LegacyRawJsonToSchemaOneMigration(), new AddPendingFieldsMigration() },
                new[] { alias }, codec, protector, SaveFailurePolicy.FailClosed);
        private sealed class AddPendingFieldsMigration : ISaveMigration
        {
            public int FromVersion => 1;
            public int ToVersion => 2;
            public JObject Migrate(JObject source)
            {
                var copy = (JObject)source.DeepClone();
                // Preserve old pendingSpinId/fingerprint and unknown members. No invented payload.
                if (copy["bw4Pending"] == null) copy["bw4Pending"] = JValue.CreateNull();
                if (copy["bw4ConsumedClaimId"] == null) copy["bw4ConsumedClaimId"] = JValue.CreateNull();
                return copy;
            }
        }
    }
}

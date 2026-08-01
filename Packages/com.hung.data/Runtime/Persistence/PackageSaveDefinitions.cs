using System;
using System.Collections.Generic;
using Hung.Base;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    public static class PackageSaveDefinitions
    {
        public const string GameDataKey = "game-data";
        public const string DailyGiftKey = "daily-gift";
        public const string HeartKey = "heart";
        public const string DailyRewardKey = "daily-reward";
        public const string PiggyBankKey = "piggy-bank";
        public const string SpinWheelKey = "spin-wheel";
        public const string RewardIntegrityKey = "reward-integrity";

        public static IReadOnlyList<SaveDefinition> CreateAll(ISaveCodec codec, ISaveProtector protector)
        {
            return new SaveDefinition[]
            {
                GameData(codec, protector),
                DailyGift(codec, protector),
                Heart(codec, protector),
                DailyReward(codec, protector),
                PiggyBank(codec, protector),
                SpinWheel(codec, protector),
                RewardIntegrity(codec, protector)
            };
        }

        public static void RegisterAll(IPersistenceService service, ISaveCodec codec, ISaveProtector protector)
        {
            service.Register(GameData(codec, protector));
            service.Register(DailyGift(codec, protector));
            service.Register(Heart(codec, protector));
            service.Register(DailyReward(codec, protector));
            service.Register(PiggyBank(codec, protector));
            service.Register(SpinWheel(codec, protector));
            service.Register(RewardIntegrity(codec, protector));
        }

        public static SaveDefinition<GameData> GameData(ISaveCodec codec, ISaveProtector protector) =>
            new SaveDefinition<GameData>(
                GameDataKey,
                1,
                CreateDefaultGameData,
                ValidateGameData,
                new ISaveMigration[] { new LegacyRawJsonToSchemaOneMigration() },
                new[] { Hung.Base.GameData.SaveKey, nameof(Hung.Base.GameData) },
                codec,
                protector,
                SaveFailurePolicy.FailClosed);

        public static SaveDefinition<DailyGiftDbModel> DailyGift(ISaveCodec codec, ISaveProtector protector) =>
            LowValue(DailyGiftKey, () => new DailyGiftDbModel
            {
                listDailyGiftStatus = new List<bool>(),
                listStreakDailyGiftStatus = new List<bool>()
            }, ValidateDailyGift, nameof(DailyGiftDbModel), codec, protector);

        public static SaveDefinition<HeartSave.HeartSaveData> Heart(ISaveCodec codec, ISaveProtector protector) =>
            LowValue(HeartKey, () => new HeartSave.HeartSaveData(), ValidateHeart, nameof(HeartSave.HeartSaveData), codec, protector);

        public static SaveDefinition<DailyRewardSaveData> DailyReward(ISaveCodec codec, ISaveProtector protector) =>
            LowValue(DailyRewardKey, () => new DailyRewardSaveData(), ValidateDailyReward, nameof(DailyRewardSaveData), codec, protector);

        public static SaveDefinition<PiggyBankSaveData> PiggyBank(ISaveCodec codec, ISaveProtector protector) =>
            LowValue(PiggyBankKey, () => new PiggyBankSaveData(), ValidatePiggyBank, nameof(PiggyBankSaveData), codec, protector);

        public static SaveDefinition<SpinWheelSaveData> SpinWheel(ISaveCodec codec, ISaveProtector protector) =>
            LowValue(SpinWheelKey, () => new SpinWheelSaveData(), ValidateSpinWheel, nameof(SpinWheelSaveData), codec, protector);

        public static SaveDefinition<RewardIntegrityStateData> RewardIntegrity(ISaveCodec codec, ISaveProtector protector) =>
            new SaveDefinition<RewardIntegrityStateData>(
                RewardIntegrityKey,
                1,
                RewardIntegrityStateData.CreateDefault,
                ValidateRewardIntegrity,
                new ISaveMigration[] { new RewardSaveMigrations() },
                new[] { nameof(RewardIntegrityStateData) },
                codec,
                protector,
                SaveFailurePolicy.FailClosed);

        private static SaveDefinition<T> LowValue<T>(
            string key,
            Func<T> createDefault,
            Func<T, SaveValidationResult> validate,
            string legacyKey,
            ISaveCodec codec,
            ISaveProtector protector) where T : new()
        {
            return new SaveDefinition<T>(
                key,
                1,
                createDefault,
                validate,
                new ISaveMigration[] { new LegacyRawJsonToSchemaOneMigration() },
                new[] { legacyKey },
                codec,
                protector,
                SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);
        }

        private static SaveValidationResult ValidateGameData(GameData value)
        {
            if (value == null || value.setting == null || value.user == null || value.level == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            if (value.user.PurchasedItems == null || value.user.ItemDatas == null || value.user.RewardGrantReceipts == null || value.level.LevelStars == null || value.level.PassLevels == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            return SaveValidationResult.Valid();
        }

        private static GameData CreateDefaultGameData()
        {
            var data = new GameData();
            data.user.PurchasedItems = new List<IAP_ITEM>();
            data.user.ItemDatas = Array.Empty<GameData.ItemData>();
            data.user.RewardGrantReceipts = new List<GameData.RewardGrantReceiptData>();
            data.level.LevelStars = new List<int>();
            data.level.PassLevels = new List<bool>();
            return data;
        }

        private static SaveValidationResult ValidateDailyGift(DailyGiftDbModel value)
        {
            if (value == null || value.listDailyGiftStatus == null || value.listStreakDailyGiftStatus == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            return NonNegative(value.dayCount, value.streakDay, value.rewardDayKey, value.cycleStartDayKey, value.currentSlot, value.lastStreakDayKey);
        }

        private static SaveValidationResult ValidateHeart(HeartSave.HeartSaveData value)
        {
            if (value == null || value.defaultMaxHearts < 0)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            return SaveValidationResult.Valid();
        }

        private static SaveValidationResult ValidateDailyReward(DailyRewardSaveData value)
        {
            if (value == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            SaveValidationResult ints = NonNegative(value.currentProgress, value.dayOfYear, value.rewardDayKey, value.lastFreeClaimTime);
            if (!ints.Success || value.lastFreeClaimUtcTicks < 0)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            return SaveValidationResult.Valid();
        }

        private static SaveValidationResult ValidatePiggyBank(PiggyBankSaveData value)
        {
            if (value == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            return NonNegative(value.currentLevelProgress);
        }

        private static SaveValidationResult ValidateSpinWheel(SpinWheelSaveData value)
        {
            if (value == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            return NonNegative(value.adsSpinToday, value.dayOfYear, value.rewardDayKey, value.spinOrdinal);
        }

        private static SaveValidationResult ValidateRewardIntegrity(RewardIntegrityStateData value)
        {
            if (value == null || value.claims == null)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            if (value.latestObservedUtcTicks < 0 || value.resetOffsetMinutes < 0 || value.resetOffsetMinutes > 1439)
                return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RewardClaimRecordData claim in value.claims)
            {
                if (claim == null ||
                    string.IsNullOrWhiteSpace(claim.claimId) ||
                    string.IsNullOrWhiteSpace(claim.feature) ||
                    string.IsNullOrWhiteSpace(claim.payloadFingerprint) ||
                    claim.createdUtcTicks < 0 ||
                    claim.updatedUtcTicks < 0 ||
                    claim.finalizedUtcTicks < 0 ||
                    claim.updatedUtcTicks < claim.createdUtcTicks ||
                    !ids.Add(claim.claimId))
                {
                    return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
                }

                if (!Enum.IsDefined(typeof(RewardClaimStateData), claim.state))
                    return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");

                if (claim.items == null)
                    return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");

                bool finalized = claim.state == (int)RewardClaimStateData.Finalized;
                if (!finalized && claim.items.Count == 0)
                    return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");

                foreach (RewardGrantItemData item in claim.items)
                {
                    if (item == null || !ItemId.TryParse(item.itemId, out _) || item.quantity <= 0)
                        return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
                }
            }

            return SaveValidationResult.Valid();
        }

        private static SaveValidationResult NonNegative(params int[] values)
        {
            foreach (int value in values)
            {
                if (value < 0)
                    return SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED");
            }

            return SaveValidationResult.Valid();
        }
    }
}

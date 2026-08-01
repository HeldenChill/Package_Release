using System;
using System.Collections.Generic;
using System.IO;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.PlayModeTests
{
    public class PersistenceRecoveryPlayModeTests
    {
        private string root;
        private PlainJsonSaveCodec codec;
        private Sha256SaveProtector protector;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath, "ComHungPersistenceTests", Guid.NewGuid().ToString("N"));
            codec = new PlainJsonSaveCodec();
            protector = new Sha256SaveProtector();
            PlayerPrefs.DeleteKey(GameData.SaveKey);
            PlayerPrefs.DeleteKey(nameof(GameData));
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(GameData.SaveKey);
            PlayerPrefs.DeleteKey(nameof(GameData));
            string fullRoot = Path.GetFullPath(root);
            string allowedRoot = Path.GetFullPath(Path.Combine(Application.temporaryCachePath, "ComHungPersistenceTests"));
            if (fullRoot.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
                Directory.Delete(fullRoot, true);
        }

        [Test]
        public void FirstBoot_CreatesCanonicalDefault()
        {
            PersistenceService service = CreateService();
            SaveDefinition<DailyRewardSaveData> definition = PackageSaveDefinitions.DailyReward(codec, protector);

            LoadResult<DailyRewardSaveData> result = service.Load(definition);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.DefaultCreated));
            Assert.That(File.Exists(PrimaryPath(PackageSaveDefinitions.DailyRewardKey)), Is.True);
        }

        [Test]
        public void SaveAndServiceRecreation_RoundTrips()
        {
            SaveDefinition<SpinWheelSaveData> definition = PackageSaveDefinitions.SpinWheel(codec, protector);
            PersistenceService first = CreateService();
            Assert.That(first.Save(definition, new SpinWheelSaveData { adsSpinToday = 4, dayOfYear = 123 }).Success, Is.True);

            PersistenceService recreated = CreateService();
            LoadResult<SpinWheelSaveData> result = recreated.Load(definition);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Value.adsSpinToday, Is.EqualTo(4));
            Assert.That(result.Value.dayOfYear, Is.EqualTo(123));
        }

        [Test]
        public void PlayerPrefsGameDataImport_RetainsLegacyAndWritesCanonical()
        {
            GameData legacy = ValidGameData();
            legacy.ClaimItem(BaseItemIds.Gold, 31);
            string json = JsonConvert.SerializeObject(legacy);
            PlayerPrefs.SetString(GameData.SaveKey, json);
            PersistenceService service = CreateService(new PlayerPrefsLegacySaveSource());

            LoadResult<GameData> result = service.Load(PackageSaveDefinitions.GameData(codec, protector));

            Assert.That(result.Success, Is.True);
            Assert.That(result.DiagnosticCode, Is.EqualTo("SAVE_LEGACY_IMPORTED"));
            Assert.That(result.Value.GetItemData(BaseItemIds.Gold).Quantity, Is.EqualTo(31));
            Assert.That(PlayerPrefs.GetString(GameData.SaveKey), Is.EqualTo(json));
            Assert.That(File.Exists(PrimaryPath(PackageSaveDefinitions.GameDataKey)), Is.True);
        }

        [Test]
        public void CorruptPrimaryWithValidBackup_RestoresBackupAndQuarantinesEvidence()
        {
            SaveDefinition<SpinWheelSaveData> definition = PackageSaveDefinitions.SpinWheel(codec, protector);
            PersistenceService service = CreateService();
            service.Save(definition, new SpinWheelSaveData { adsSpinToday = 2 });
            service.Save(definition, new SpinWheelSaveData { adsSpinToday = 8 });
            File.WriteAllText(PrimaryPath(definition.Key), "corrupt-primary");

            LoadResult<SpinWheelSaveData> result = CreateService().Load(definition);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.BackupRestored));
            Assert.That(result.Value.adsSpinToday, Is.EqualTo(2));
            Assert.That(Directory.GetFiles(Path.Combine(root, "quarantine")).Length, Is.EqualTo(1));
        }

        [Test]
        public void AllProductionDefinitions_RoundTrip()
        {
            PersistenceService service = CreateService();
            AssertRoundTrip(service, PackageSaveDefinitions.GameData(codec, protector), ValidGameData(), value => value.user.ItemDatas.Length == 1);
            AssertRoundTrip(service, PackageSaveDefinitions.DailyGift(codec, protector), new DailyGiftDbModel
            {
                dayCount = 2,
                streakDay = 1,
                listDailyGiftStatus = new List<bool> { true },
                listStreakDailyGiftStatus = new List<bool> { false }
            }, value => value.dayCount == 2);
            AssertRoundTrip(service, PackageSaveDefinitions.Heart(codec, protector), new HeartSave.HeartSaveData
            {
                defaultMaxHearts = 5,
                addMaxHearts = 2
            }, value => value.addMaxHearts == 2);
            AssertRoundTrip(service, PackageSaveDefinitions.DailyReward(codec, protector), new DailyRewardSaveData
            {
                currentProgress = 3,
                dayOfYear = 120,
                lastFreeClaimTime = 9
            }, value => value.currentProgress == 3);
            AssertRoundTrip(service, PackageSaveDefinitions.PiggyBank(codec, protector), new PiggyBankSaveData
            {
                currentLevelProgress = 7
            }, value => value.currentLevelProgress == 7);
            AssertRoundTrip(service, PackageSaveDefinitions.SpinWheel(codec, protector), new SpinWheelSaveData
            {
                adsSpinToday = 4,
                dayOfYear = 121
            }, value => value.adsSpinToday == 4);
        }

        private PersistenceService CreateService(ILegacySaveSource legacy = null) =>
            new PersistenceService(new FileSaveStore(root), legacy);

        private string PrimaryPath(string key) => Path.Combine(root, "primary", key + ".save");

        private static GameData ValidGameData()
        {
            var data = new GameData();
            data.InitData(new[] { BaseItemIds.Gold });
            return data;
        }

        private static void AssertRoundTrip<T>(
            PersistenceService service,
            SaveDefinition<T> definition,
            T value,
            Func<T, bool> assertion) where T : new()
        {
            Assert.That(service.Save(definition, value).Success, Is.True, definition.Key);
            LoadResult<T> loaded = service.Load(definition);
            Assert.That(loaded.Success, Is.True, definition.Key);
            Assert.That(assertion(loaded.Value), Is.True, definition.Key);
        }
    }
}

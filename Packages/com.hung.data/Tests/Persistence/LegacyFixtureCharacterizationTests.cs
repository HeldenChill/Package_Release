using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hung.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests.Persistence
{
    public class LegacyFixtureCharacterizationTests
    {
        private static readonly JsonSerializerSettings LegacyFieldSettings = new()
        {
            ContractResolver = new LegacyFieldContractResolver()
        };

        [TestCase("Persistence/Fixtures/GameData.item-id-v1.raw", typeof(GameData))]
        [TestCase("Persistence/Fixtures/DailyGiftDbModel.raw", typeof(DailyGiftDbModel))]
        [TestCase("Persistence/Fixtures/HeartSaveData.raw", typeof(HeartSave.HeartSaveData))]
        [TestCase("Persistence/Fixtures/DailyRewardSaveData.raw", typeof(DailyRewardSaveData))]
        [TestCase("Persistence/Fixtures/PiggyBankSaveData.raw", typeof(PiggyBankSaveData))]
        [TestCase("Persistence/Fixtures/SpinWheelSaveData.raw", typeof(SpinWheelSaveData))]
        public void RawFixture_DeserializesWithoutChangingShape(string resourceName, Type modelType)
        {
            string json = Resources.Load<TextAsset>(resourceName).text;
            object value = JsonConvert.DeserializeObject(json, modelType);
            Assert.That(value, Is.Not.Null);
            Assert.That(
                JObject.Parse(JsonConvert.SerializeObject(value, LegacyFieldSettings)),
                Is.EqualTo(JObject.Parse(json)));
        }

        [Test]
        public void ProductionModelInventory_MapsCanonicalAndLegacyKeys()
        {
            var expected = new Dictionary<Type, string[]>
            {
                [typeof(GameData)] = new[] { "GameData.item-id-v1", "GameData" },
                [typeof(DailyGiftDbModel)] = new[] { "DailyGiftDbModel" },
                [typeof(HeartSave.HeartSaveData)] = new[] { "HeartSaveData" },
                [typeof(DailyRewardSaveData)] = new[] { "DailyRewardSaveData" },
                [typeof(PiggyBankSaveData)] = new[] { "PiggyBankSaveData" },
                [typeof(SpinWheelSaveData)] = new[] { "SpinWheelSaveData" },
            };

            Assert.That(expected[typeof(GameData)], Is.EquivalentTo(new[] { GameData.SaveKey, nameof(GameData) }));
        }

        private sealed class LegacyFieldContractResolver : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                return objectType
                    .GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Cast<MemberInfo>()
                    .ToList();
            }
        }
    }
}

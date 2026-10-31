using System.IO;
using Hung.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hung.Data.Tests
{
    public sealed class GameConfigCompatibilityTests
    {
        private const string AssetPath =
            "Packages/com.hung.data/Runtime/DataSO/GameConfig.asset";
        private const string ScriptPath =
            "Packages/com.hung.data/Runtime/DataSO/GameConfig.cs";
        private const string ScriptGuid = "4d9cfb4bb37db1e43a85d2b6a9a59105";

        [Test]
        public void PackageAsset_UsesExistingGameConfigScript()
        {
            string yaml = File.ReadAllText(AssetPath);
            StringAssert.Contains("m_Script: {fileID: 11500000, guid: " +
                ScriptGuid + ", type: 3}", yaml);
            Assert.That(AssetDatabase.GUIDToAssetPath(ScriptGuid), Is.EqualTo(ScriptPath));
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(ScriptPath).GetClass(),
                Is.EqualTo(typeof(global::GameConfig)));
        }

        [Test]
        public void PackageAsset_LoadsAsGameConfig()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<global::GameConfig>(AssetPath),
                Is.Not.Null);
        }

        [Test]
        public void PackageAsset_RetainsPolicyValues()
        {
            var config = AssetDatabase.LoadAssetAtPath<global::GameConfig>(AssetPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.StartInterLevel, Is.EqualTo(8));
            Assert.That(config.ShowInterLevelStep, Is.EqualTo(2));
            Assert.That(config.InterCappingTime, Is.EqualTo(30f));
            Assert.That(config.AdsCappingCount, Is.EqualTo(4));
        }

        [Test]
        public void PackageAsset_RetainsAssetIdentity()
        {
            Assert.That(AssetDatabase.AssetPathToGUID(AssetPath),
                Is.EqualTo("523180917bcedcf4b92cb29125de343b"));
            var config = AssetDatabase.LoadAssetAtPath<global::GameConfig>(AssetPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                config, out string guid, out long fileId), Is.True);
            Assert.That(guid, Is.EqualTo("523180917bcedcf4b92cb29125de343b"));
            Assert.That(fileId, Is.EqualTo(11400000L));
        }

        [Test]
        public void GameConfig_RetainsPublicTypeAndSerializedFields()
        {
            Assert.That(typeof(global::GameConfig).FullName, Is.EqualTo("GameConfig"));
            Assert.That(typeof(global::GameConfig).Assembly.GetName().Name,
                Is.EqualTo("Hung.Data"));
            var config = ScriptableObject.CreateInstance<global::GameConfig>();
            try
            {
                var serialized = new SerializedObject(config);
                foreach (string name in new[] { "startInterLevel", "showInterLevelStep",
                    "interCappingTime", "adsCappingCount" })
                {
                    var property = serialized.FindProperty(name);
                    Assert.That(property, Is.Not.Null, name);
                    Assert.That(property.propertyType,
                        Is.EqualTo(SerializedPropertyType.Integer), name);
                }
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void DataManager_ReturnsAssignedGameConfigWithoutLoadingSaves()
        {
            var go = new GameObject("H1D-Inactive-Data-Probe");
            go.SetActive(false);
            var config = ScriptableObject.CreateInstance<global::GameConfig>();
            try
            {
                var manager = go.AddComponent<DataManager>();
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("gameConfig").objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(manager.GetSOData<global::GameConfig>(), Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(config);
            }
        }
    }
}


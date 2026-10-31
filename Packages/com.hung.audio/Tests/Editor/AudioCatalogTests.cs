using System;
using Hung.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hung.Audio.Editor.Tests
{
    public sealed class AudioCatalogTests
    {
        private AudioCatalog catalog;
        private AudioClip clip;

        [SetUp] public void SetUp()
        {
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            clip = AudioClip.Create("cue", 4410, 1, 44100, false);
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(clip);
        }
        internal static void SetRows(AudioCatalog target, params AudioCueDefinition[] rows)
        {
            var serialized = new SerializedObject(target);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = entries.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Id").FindPropertyRelative("value").stringValue = rows[i].Id.Value;
                row.FindPropertyRelative("CodeName").stringValue = rows[i].CodeName;
                row.FindPropertyRelative("Clip").objectReferenceValue = rows[i].Clip;
                row.FindPropertyRelative("Volume").floatValue = rows[i].Volume;
                row.FindPropertyRelative("Kind").intValue = (int)rows[i].Kind;
                row.FindPropertyRelative("MixerGroup").objectReferenceValue = rows[i].MixerGroup;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private AudioCueDefinition Row() => new AudioCueDefinition
        { Id = BaseAudioIds.UiClick, CodeName = "UiClick", Clip = clip, Volume = 0.4f };

        [Test] public void DuplicateIdsFailBeforeReturningIndex()
        {
            SetRows(catalog, Row(), Row());
            StringAssert.Contains("entry 1", Assert.Throws<InvalidOperationException>(() => catalog.BuildIndex()).Message);
        }
        [Test] public void RuntimeSnapshotDoesNotFollowAssetMutation()
        {
            SetRows(catalog, Row());
            var before = catalog.BuildIndex();
            var changed = Row(); changed.Volume = 0.9f;
            SetRows(catalog, changed);
            Assert.That(before[BaseAudioIds.UiClick].Volume, Is.EqualTo(0.4f));
            Assert.That(catalog.BuildIndex()[BaseAudioIds.UiClick].Volume, Is.EqualTo(0.9f));
        }
        [TestCase(-1f)] [TestCase(2f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidGainFails(float volume)
        {
            var row = Row(); row.Volume = volume; SetRows(catalog, row);
            Assert.Throws<InvalidOperationException>(() => catalog.BuildIndex());
        }
        [Test] public void WrongMusicPrefixFails()
        {
            var row = Row(); row.Kind = AudioCueKind.Music; SetRows(catalog, row);
            Assert.Throws<InvalidOperationException>(() => catalog.BuildIndex());
        }
        [Test] public void InvalidIdAndMissingClipFail()
        {
            var row = Row(); row.Id = default; SetRows(catalog, row);
            Assert.Throws<InvalidOperationException>(() => catalog.BuildIndex());
            row = Row(); row.Clip = null; SetRows(catalog, row);
            Assert.Throws<InvalidOperationException>(() => catalog.BuildIndex());
        }
        [Test] public void CatalogSurvivesRealAssetRoundTrip()
        {
            string folder = "Assets/AudioCatalogProof_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            try
            {
                const string clipPath = "Packages/com.hung.audio/Runtime/Audio/OGG/Button_Click.ogg";
                var savedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                Assert.That(savedClip, Is.Not.Null, "Imported package clip required for asset-reference proof.");
                var saved = ScriptableObject.CreateInstance<AudioCatalog>();
                var row = Row(); row.Clip = savedClip; SetRows(saved, row);
                AssetDatabase.CreateAsset(saved, folder + "/catalog.asset");
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(saved);
                AssetDatabase.ImportAsset(folder + "/catalog.asset", ImportAssetOptions.ForceUpdate);
                var reloaded = AssetDatabase.LoadAssetAtPath<AudioCatalog>(folder + "/catalog.asset");
                var cue = reloaded.BuildIndex()[BaseAudioIds.UiClick];
                Assert.That(cue.Volume, Is.EqualTo(0.4f));
                Assert.That(AssetDatabase.GetAssetPath(cue.Clip), Is.EqualTo(clipPath));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}

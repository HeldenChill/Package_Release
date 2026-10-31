using System;
using System.Linq;
using Hung.Base;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Audio.Editor.Tests
{
    public sealed class AudioPluginTests
    {
        private AudioCatalog catalog;
        private AudioPluginConfiguration config;
        private AudioClip clip;
        private FeatureHost host;
        [SetUp] public void SetUp()
        {
            ServiceSlots.ResetForTests();
            clip = AudioClip.Create("cue", 4410, 1, 44100, false);
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            AudioCatalogTests.SetRows(catalog, new AudioCueDefinition { Id = BaseAudioIds.UiClick, CodeName = "UiClick", Clip = clip });
            config = ScriptableObject.CreateInstance<AudioPluginConfiguration>();
            var serialized = new SerializedObject(config); serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo(); host = new FeatureHost();
        }
        [TearDown] public void TearDown()
        {
            host.Dispose(); ServiceSlots.ResetForTests();
            UnityEngine.Object.DestroyImmediate(config); UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(clip);
        }
        [Test] public void MissingAndDuplicateConfigurationFailWithoutOwner()
        {
            host.Register(AudioPlugin.CreateRegistration(() => Array.Empty<AudioPluginConfiguration>()));
            Assert.That(host.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_CONFIG_MISSING"));
            Assert.That(ServiceSlots.OwnerOf<IAudioService>(), Is.Null); host.Dispose(); host = new FeatureHost();
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config, config }));
            Assert.That(host.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_CONFIG_DUPLICATE"));
        }
        [Test] public void ExistingOwnerRefusedBeforeConfigurationRead()
        {
            var owner = new UnsupportedAudioService(); ServiceSlots.Register<IAudioService>(owner);
            int reads = 0;
            host.Register(AudioPlugin.CreateRegistration(() => { reads++; return new[] { config }; }));
            Assert.That(host.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_OWNER_PRESENT"));
            Assert.That(reads, Is.Zero); Assert.That(Locator.Audio, Is.SameAs(owner));
        }
        [Test] public void StartPublishesHungAudioAndShutdownRevealsFallback()
        {
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config }));
            Assert.That(host.Start().Features[0].State, Is.EqualTo(FeatureState.Ready));
            Assert.That(ServiceSlots.OwnerOf<IAudioService>(), Is.EqualTo("Hung.Audio"));
            var old = Locator.Audio;
            host.Dispose(); host.Dispose();
            Assert.That(ServiceSlots.OwnerOf<IAudioService>(), Is.Null);
            Assert.That(Locator.Audio, Is.TypeOf<UnsupportedAudioService>());
            Assert.That(ServiceSlots.Unregister(old), Is.False);
        }
        [Test] public void SameAssemblySecondSessionCannotReplaceLiveOwner()
        {
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config })); host.Start();
            var first = Locator.Audio;
            using (var second = new FeatureHost())
            {
                second.Register(AudioPlugin.CreateRegistration(() => new[] { config }));
                Assert.That(second.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_OWNER_PRESENT"));
                Assert.That(Locator.Audio, Is.SameAs(first));
            }
        }
        [Test] public void FallbackWarnsOncePerMemberAndResetRestoresBudget()
        {
            LogAssert.Expect(LogType.Warning, "[Unsupported] IAudioService.PlaySfx called; no audio plugin installed.");
            Locator.Audio.PlaySfx(BaseAudioIds.UiClick); Locator.Audio.PlaySfx(BaseAudioIds.UiClick);
            Assert.That(ServiceSlots.TryGet<IAudioService>(out _), Is.False);
            ServiceSlots.ResetForTests();
            LogAssert.Expect(LogType.Warning, "[Unsupported] IAudioService.PlaySfx called; no audio plugin installed.");
            Locator.Audio.PlaySfx(BaseAudioIds.UiClick);
            Assert.That(Locator.Audio.ClipLength(default), Is.Zero);
        }
        [Test] public void MissingCatalogAndInvalidCapFailBeforePublishing()
        {
            var serialized = new SerializedObject(config); serialized.FindProperty("catalog").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config }));
            Assert.That(host.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_CATALOG_MISSING"));
            host.Dispose(); host = new FeatureHost();
            serialized.FindProperty("catalog").objectReferenceValue = catalog; serialized.FindProperty("maxSfxVoices").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config }));
            Assert.That(host.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_VOICE_CAP_INVALID"));
            Assert.That(ServiceSlots.TryGet<IAudioService>(out _), Is.False);
        }
        [Test] public void InvalidCatalogFailsWithNamedDiagnostic()
        {
            AudioCatalogTests.SetRows(catalog, new AudioCueDefinition { Id = BaseAudioIds.UiClick, CodeName = "UiClick" });
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("\\[Audio\\] catalog validation failed:"));
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config }));
            Assert.That(host.Start().Features[0].DiagnosticCode, Is.EqualTo("AUDIO_CATALOG_INVALID"));
            Assert.That(ServiceSlots.TryGet<IAudioService>(out _), Is.False);
        }
        [Test] public void ShutdownCannotUnregisterReplacementFromSameAssembly()
        {
            host.Register(AudioPlugin.CreateRegistration(() => new[] { config })); host.Start();
            var root = new GameObject("replacement"); var core = root.AddComponent<AudioPlayback>();
            try
            {
                core.Initialize(catalog); var replacement = new AudioServiceAdapter(core);
                Assert.That(ServiceSlots.Register<IAudioService>(replacement), Is.EqualTo(ServiceRegistration.Replaced));
                host.Dispose(); Assert.That(Locator.Audio, Is.SameAs(replacement));
            }
            finally { core.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test] public void SuppressionDoesNotReadConfigurationOrCreateRoot()
        {
            int before = Resources.FindObjectsOfTypeAll<AudioPlayback>().Length;
            using (PluginStartupPolicy.OverrideForTests(AudioPlugin.FeatureId, false))
                typeof(AudioPlugin).GetMethod("Declare", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.That(Resources.FindObjectsOfTypeAll<AudioPlayback>().Length, Is.EqualTo(before));
            Assert.That(ServiceSlots.OwnerOf<IAudioService>(), Is.Null);
        }

        [Test] public void BuildValidationRejectsConfigurationInHungAudioResourceFolder()
        {
            WithResourceConfiguration(false, path =>
            {
                Assert.That(Resources.LoadAll<AudioPluginConfiguration>("HungAudio")
                    .Any(value => AssetDatabase.GetAssetPath(value) == path), Is.True);
                var error = Assert.Throws<UnityEditor.Build.BuildFailedException>(() => AudioConfigurationValidator.Validate());
                StringAssert.Contains("AUDIO_CONFIG_DUPLICATE", error.Message);
                StringAssert.Contains(path, error.Message);
            });
        }

        [Test] public void BuildValidationIgnoresEditorOnlyResourceConfiguration()
        {
            WithResourceConfiguration(true, path => Assert.DoesNotThrow(() => AudioConfigurationValidator.Validate()));
        }

        private static void WithResourceConfiguration(bool editorOnly, Action<string> check)
        {
            string folder = "AudioValidatorProof_" + Guid.NewGuid().ToString("N");
            string root = "Assets/" + folder;
            AssetDatabase.CreateFolder("Assets", folder);
            try
            {
                string parent = root;
                if (editorOnly) { AssetDatabase.CreateFolder(parent, "Editor"); parent += "/Editor"; }
                AssetDatabase.CreateFolder(parent, "Resources"); parent += "/Resources";
                if (!editorOnly) { AssetDatabase.CreateFolder(parent, "HungAudio"); parent += "/HungAudio"; }
                string path = parent + (editorOnly ? "/HungAudio.asset" : "/Extra.asset");
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AudioPluginConfiguration>(), path);
                AssetDatabase.SaveAssets();
                using (PluginStartupPolicy.OverrideForTests(AudioPlugin.FeatureId, true)) check(path);
            }
            finally { AssetDatabase.DeleteAsset(root); }
        }
    }
}

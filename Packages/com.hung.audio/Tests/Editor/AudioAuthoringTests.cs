using System;
using System.Linq;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Audio.Editor.Tests
{
    public sealed class AudioAuthoringTests
    {
        [Test] public void BaseCatalogOptionUsesSameLabelAsConstants()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            var clip = AudioClip.Create("click", 4410, 1, 44100, false);
            try
            {
                AudioCatalogTests.SetRows(catalog, new AudioCueDefinition
                { Id = BaseAudioIds.UiClick, CodeName = "UiClick", Clip = clip });
                var source = new AudioCatalogEditorSource(() => new[] { catalog });
                Assert.That(source.GetOptions().Single().Label, Is.EqualTo("UiClick"));
                var constants = new Hung.Base.Editor.StableIdConstantsSource<AudioId>(typeof(BaseAudioIds));
                Hung.Base.Editor.StableIdEditorRegistry<AudioId>.Build(
                    new Hung.Base.Editor.IStableIdEditorSource<AudioId>[] { constants, source }, out var conflicts);
                Assert.That(conflicts, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(clip); }
        }
        [Test] public void GeneratorRejectsEquivalentEscapedNames()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            var clip = AudioClip.Create("click", 4410, 1, 44100, false);
            try
            {
                AudioCatalogTests.SetRows(catalog,
                    new AudioCueDefinition { Id = new AudioId("test.hit"), CodeName = "Hit", Clip = clip },
                    new AudioCueDefinition { Id = new AudioId("test.other"), CodeName = "@Hit", Clip = clip });
                Assert.Throws<InvalidOperationException>(() => AudioCatalogCodeGenerator.GenerateContent(catalog));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(clip); }
        }
        [Test] public void GeneratorSkipsBaseConstantsAndKeepsProductIds()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            var clip = AudioClip.Create("click", 4410, 1, 44100, false);
            try
            {
                AudioCatalogTests.SetRows(catalog,
                    new AudioCueDefinition { Id = BaseAudioIds.UiClick, CodeName = "UiClick", Clip = clip },
                    new AudioCueDefinition { Id = new AudioId("test.hit"), CodeName = "Hit", Clip = clip });
                string code = AudioCatalogCodeGenerator.GenerateContent(catalog);
                StringAssert.Contains("test.hit", code); StringAssert.DoesNotContain("base.ui.click", code);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(clip); }
        }
    }
}

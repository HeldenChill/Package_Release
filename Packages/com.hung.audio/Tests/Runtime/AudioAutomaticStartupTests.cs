using System;
using System.Collections;
using System.Linq;
using Hung.Base;
using Hung.Base.Composition;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hung.Audio.Tests
{
    public sealed class AudioAutomaticStartupTests
    {
        [UnityTest] public IEnumerator AutoOwnerSurvivesSceneTransitionAndPlaysMappedCue()
        {
            yield return null;
            var feature = AutomaticPluginHost.GetReport().Features.Single(x => x.Id == AudioPlugin.FeatureId);
            Assert.That(feature.State, Is.EqualTo(FeatureState.Ready));
            Assert.That(ServiceSlots.OwnerOf<IAudioService>(), Is.EqualTo("Hung.Audio"));
            var roots = Resources.FindObjectsOfTypeAll<AudioPlayback>().Where(x => x != null && x.gameObject.name == "Hung.Audio.Session").ToArray();
            Assert.That(roots.Length, Is.EqualTo(1)); var core = roots[0];
            var config = Resources.Load<AudioPluginConfiguration>("HungAudio");
            Assert.That(core.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(config.MaxSfxVoices + 1));
            var cue = config.Catalog.BuildIndex().Values.FirstOrDefault(x => x.Kind == AudioCueKind.Sfx);
            if (cue == null) Assert.Inconclusive("AUDIO_CONFIGURATION_GAP: migrated product catalog has no mapped SFX clips; production playback is unverified.");
            AudioSource loop = null;
            try
            {
                loop = Locator.Audio.PlayLoopSfx(cue.Id, 0);
                Assert.That(loop, Is.Not.Null); Assert.That(loop.clip, Is.SameAs(cue.Clip));
                var scene = SceneManager.CreateScene("AudioTransitionProof");
                yield return SceneManager.UnloadSceneAsync(scene);
                Assert.That(core != null, Is.True); Assert.That(core.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
                Debug.Log("AUDIO_AUTOMATIC_SMOKE owner=Hung.Audio roots=1 sources=" + core.GetComponentsInChildren<AudioSource>().Length);
            }
            finally
            {
                Locator.Audio.StopLoopSfx(loop, 0);
            }
        }

    }
}

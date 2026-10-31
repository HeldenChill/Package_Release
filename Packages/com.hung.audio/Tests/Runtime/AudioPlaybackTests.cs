using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Audio.Tests
{
    public sealed class AudioPlaybackTests
    {
        private sealed class Settings : IAudioSettings
        {
            public bool Bgm; public bool Sfx;
            public bool IsBgmMuted => Bgm;
            public bool IsSfxMuted => Sfx;
        }
        private GameObject root;
        private AudioCatalog catalog;
        private AudioClip clip;
        private AudioClip clipB;
        private AudioPlayback playback;
        private Settings settings;
        private static readonly AudioId MusicA = new AudioId("bgm.test.a");
        private static readonly AudioId MusicB = new AudioId("bgm.test.b");

        [SetUp] public void SetUp()
        {
            clip = AudioClip.Create("cue", 4410, 1, 44100, false);
            clipB = AudioClip.Create("cue B", 8820, 1, 44100, false);
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            var rows = new List<AudioCueDefinition>();
            foreach (var id in new[] { BaseAudioIds.UiClick, MusicA, MusicB })
                rows.Add(new AudioCueDefinition { Id = id, CodeName = id == BaseAudioIds.UiClick ? "Click" :
                    id == MusicA ? "MusicA" : "MusicB", Clip = id == MusicB ? clipB : clip, Volume = 0.4f,
                    Kind = id == BaseAudioIds.UiClick ? AudioCueKind.Sfx : AudioCueKind.Music });
            typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog, rows);
            root = new GameObject("AudioPlaybackProof");
            playback = root.AddComponent<AudioPlayback>(); settings = new Settings();
            playback.Initialize(catalog, settings, 1);
        }
        [TearDown] public void TearDown()
        {
            playback.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(clip);
            UnityEngine.Object.DestroyImmediate(clipB);
        }
        [Test] public void VoiceCapRejectsNewestWithoutAllocating()
        {
            var source = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            Assert.That(source, Is.Not.Null);
            Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick, 0), Is.Null);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(2));
        }
        [Test] public void ForeignStopCannotReleaseOwnedLoop()
        {
            var owned = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            var foreignRoot = new GameObject("foreign");
            try
            {
                playback.StopLoopSfx(foreignRoot.AddComponent<AudioSource>(), 0);
                Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick, 0), Is.Null);
                playback.StopLoopSfx(owned, 0);
                Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick, 0), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(foreignRoot); }
        }
        [UnityTest] public IEnumerator LiveSettingsRebindAndUnbindApplyToLoop()
        {
            var source = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            settings.Sfx = true; yield return null;
            Assert.That(source.mute, Is.True);
            playback.BindSettings(new Settings()); Assert.That(source.mute, Is.False);
            playback.ToggleSfxVolume(true); Assert.That(source.mute, Is.True);
            playback.UnbindSettings(); Assert.That(source.mute, Is.False);
            Assert.Throws<ArgumentNullException>(() => playback.BindSettings(null));
        }
        [Test] public void SpatialAndPitchStateResetOnReuse()
        {
            var source = playback.PlaySfx(BaseAudioIds.UiClick, new Vector3(2, 3, 4), 0.5f, 2f);
            Assert.That(source.transform.position, Is.EqualTo(new Vector3(2, 3, 4)));
            Assert.That(source.spatialBlend, Is.EqualTo(1));
            Assert.That(source.volume, Is.EqualTo(0.2f).Within(0.001f));
            playback.StopSfx();
            var loop = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            Assert.That(loop, Is.SameAs(source));
            Assert.That(loop.spatialBlend, Is.Zero); Assert.That(loop.pitch, Is.EqualTo(1));
            Assert.That(loop.volume, Is.EqualTo(0.4f).Within(0.001f));
        }
        [UnityTest] public IEnumerator OneShotReclaimsAfterActualCompletion()
        {
            Assert.That(playback.PlaySfx(BaseAudioIds.UiClick), Is.Not.Null);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick, 0), Is.Not.Null);
        }
        [UnityTest] public IEnumerator FirstBgmAndStopCancelPendingReplacement()
        {
            playback.PlayBgm(MusicA);
            AudioSource music = null;
            foreach (var source in root.GetComponentsInChildren<AudioSource>())
                if (source.gameObject.name == "BGM") music = source;
            Assert.That(music, Is.Not.Null); Assert.That(music.clip, Is.SameAs(clip));
            playback.PlayBgm(MusicB, 0.05f); playback.StopBgm();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(music.clip, Is.Null); Assert.That(music.isPlaying, Is.False);
        }
        [Test] public void InvalidRequestsLeaveOwnedVoiceUnchanged()
        {
            var source = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            Assert.That(playback.PlaySfx(default), Is.Null);
            Assert.That(playback.PlaySfx(MusicA), Is.Null);
            Assert.That(playback.PlaySfx(BaseAudioIds.UiClick, Vector3.zero, float.NaN, 1), Is.Null);
            playback.StopSfx(default);
            Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick, 0), Is.Null);
            Assert.That(playback.ClipLength(default), Is.Zero);
            Assert.That(playback.ClipLength(BaseAudioIds.UiClick), Is.EqualTo(0.1f).Within(0.001f));
            playback.StopLoopSfx(source, 0);
        }
        [UnityTest] public IEnumerator DisposeIsIdempotentAndDestroysOnlyOwnedSources()
        {
            var foreign = root.AddComponent<AudioSource>();
            playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            playback.Dispose(); playback.Dispose(); yield return null;
            Assert.That(foreign, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1));
            Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick), Is.Null);
        }
        private AudioSource MusicSource() => Array.Find(root.GetComponentsInChildren<AudioSource>(),
            source => source.gameObject.name == "BGM");
        [UnityTest] public IEnumerator LatestValidMusicRequestCancelsOlderReplacement()
        {
            playback.PlayBgm(MusicA, 0);
            playback.PlayBgm(MusicB, 0.05f);
            playback.PlayBgm(MusicA, 0.05f);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(MusicSource().clip, Is.SameAs(clip));
            playback.PlayBgm(MusicB, 0.05f);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(MusicSource().clip, Is.SameAs(clipB));
        }
        [UnityTest] public IEnumerator PauseFreezesPendingMusicReplacement()
        {
            playback.PlayBgm(MusicA, 0); playback.PlayBgm(MusicB, 0.05f); playback.PauseBgm();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(MusicSource().clip, Is.SameAs(clip));
            playback.UnPauseBgm();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(MusicSource().clip, Is.SameAs(clipB));
        }
        [UnityTest] public IEnumerator LoopFadeStopReleasesExactlyOnce()
        {
            var source = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0.02f);
            playback.StopLoopSfx(source, 0.02f); playback.StopLoopSfx(source, 0.02f);
            yield return new WaitForSecondsRealtime(0.08f);
            var reused = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            Assert.That(reused, Is.SameAs(source));
            Assert.That(playback.PlayLoopSfx(BaseAudioIds.UiClick, 0), Is.Null);
        }
        [Test] public void EmptyRandomAndInvalidPitchDoNotAlterOwnedVoice()
        {
            Assert.That(playback.PlayRandomSfx(null), Is.Null);
            Assert.That(playback.PlayRandomSfx(Array.Empty<AudioId>()), Is.Null);
            var source = playback.PlayLoopSfx(BaseAudioIds.UiClick, 0);
            playback.SetPitch(source, 2); Assert.That(source.pitch, Is.EqualTo(2));
            playback.SetPitch(source, float.NaN); Assert.That(source.pitch, Is.EqualTo(2));
            Assert.That(playback.ClipLength(BaseAudioIds.UiClick), Is.EqualTo(0.1f).Within(0.001f));
        }
        [Test] public void InvalidInitializationAllocatesNoSources()
        {
            var invalid = ScriptableObject.CreateInstance<AudioCatalog>();
            var otherRoot = new GameObject("invalid Audio owner");
            try
            {
                var other = otherRoot.AddComponent<AudioPlayback>();
                Assert.Throws<ArgumentOutOfRangeException>(() => other.Initialize(invalid, null, 0));
                Assert.That(otherRoot.GetComponentsInChildren<AudioSource>(), Is.Empty);
                typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(invalid, new List<AudioCueDefinition> { new AudioCueDefinition() });
                Assert.Throws<InvalidOperationException>(() => other.Initialize(invalid));
                Assert.That(otherRoot.GetComponentsInChildren<AudioSource>(), Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(otherRoot); UnityEngine.Object.DestroyImmediate(invalid); }
        }
    }
}

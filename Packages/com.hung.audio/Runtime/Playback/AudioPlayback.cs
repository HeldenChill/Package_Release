using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.Audio
{
    /// <summary>Opt-in bounded playback owner; does not register or discover application services.</summary>
    public sealed class AudioPlayback : MonoBehaviour, IDisposable
    {
        private sealed class Voice
        {
            internal AudioSource Source;
            internal AudioCue Cue;
            internal float Gain, Scale, FadeRemaining, FadeDuration, FadeStart;
            internal bool Releasing;
        }
        private readonly List<Voice> voices = new List<Voice>();
        private readonly List<GameObject> children = new List<GameObject>();
        private IReadOnlyDictionary<AudioId, AudioCue> cues;
        private AudioSource bgm;
        private AudioCue music, pendingMusic;
        private IAudioSettings settings;
        private bool? bgmOverride, sfxOverride;
        private float musicGain = 1, musicFadeRemaining, musicFadeDuration, musicFadeStart;
        private bool initialized, disposed, paused;

        /// <summary>Validates and initializes once, with one music source and a fixed SFX capacity.</summary>
        public void Initialize(AudioCatalog catalog, IAudioSettings initialSettings = null, int maxVoices = 16)
        {
            if (initialized || disposed) throw new InvalidOperationException("AudioPlayback initializes once.");
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (maxVoices < 1 || maxVoices > 128) throw new ArgumentOutOfRangeException(nameof(maxVoices));
            var snapshot = catalog.BuildIndex();
            cues = snapshot; settings = initialSettings; initialized = true;
            try
            {
                bgm = CreateSource("BGM"); bgm.loop = true;
                for (int i = 0; i < maxVoices; i++) voices.Add(new Voice { Source = CreateSource("SFX " + i) });
                ApplySettings();
            }
            catch { Dispose(); throw; }
        }
        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName); children.Add(child);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>(); source.playOnAwake = false;
            return source;
        }
        private bool Ready => initialized && !disposed;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private bool TryCue(AudioId id, AudioCueKind kind, out AudioCue cue)
        {
            cue = null;
            return Ready && id.IsValid && cues.TryGetValue(id, out cue) && cue.Kind == kind && cue.Clip != null;
        }
        private Voice Acquire(AudioId id, bool loop, Vector3 position, bool spatial, float scale, float pitch)
        {
            if (!Finite(scale) || scale < 0 || scale > 1 || !Finite(pitch) || pitch < 0.01f || pitch > 3 ||
                !Finite(position.x) || !Finite(position.y) || !Finite(position.z) ||
                !TryCue(id, AudioCueKind.Sfx, out var cue)) return null;
            foreach (var voice in voices)
            {
                if (voice.Cue != null) continue;
                var source = voice.Source;
                if (source == null) continue;
                source.transform.localPosition = Vector3.zero;
                if (spatial) source.transform.position = position;
                source.clip = cue.Clip; source.outputAudioMixerGroup = cue.MixerGroup;
                source.loop = loop; source.pitch = pitch; source.spatialBlend = spatial ? 1 : 0;
                voice.Cue = cue; voice.Scale = scale; voice.Gain = 1;
                voice.FadeRemaining = 0; voice.Releasing = false;
                ApplyVoice(voice); source.Play(); return voice;
            }
            return null;
        }
        /// <summary>Plays one 2D cue; null means invalid, unknown, or capacity exhausted.</summary>
        public AudioSource PlaySfx(AudioId id) => Acquire(id, false, Vector3.zero, false, 1, 1)?.Source;
        /// <summary>Plays a spatial cue with bounded gain and positive pitch.</summary>
        public AudioSource PlaySfx(AudioId id, Vector3 position, float volume = 1, float pitch = 1) =>
            Acquire(id, false, position, true, volume, pitch)?.Source;
        /// <summary>Leases a looping 2D voice until stopped.</summary>
        public AudioSource PlayLoopSfx(AudioId id, float fadeIn = 0.1f)
        {
            if (!Finite(fadeIn) || fadeIn < 0) return null;
            var voice = Acquire(id, true, Vector3.zero, false, 1, 1);
            if (voice == null) return null;
            if (fadeIn > 0)
            {
                voice.Gain = 0; voice.FadeStart = 0;
                voice.FadeDuration = voice.FadeRemaining = fadeIn; ApplyVoice(voice);
            }
            return voice.Source;
        }
        private Voice FindVoice(AudioSource source)
        {
            if (!Ready || source == null) return null;
            foreach (var voice in voices)
                if (voice.Cue != null && ReferenceEquals(voice.Source, source)) return voice;
            return null;
        }
        /// <summary>Stops only an active loop owned by this playback instance.</summary>
        public void StopLoopSfx(AudioSource source, float fadeOut = 0.1f)
        {
            var voice = FindVoice(source);
            if (voice == null || !source.loop || voice.Releasing || !Finite(fadeOut) || fadeOut < 0) return;
            if (fadeOut == 0) { Release(voice); return; }
            voice.Releasing = true; voice.FadeStart = voice.Gain;
            voice.FadeDuration = voice.FadeRemaining = fadeOut;
        }
        /// <summary>Stops every owned sound effect, including loops.</summary>
        public void StopSfx() { foreach (var voice in voices) Release(voice); }
        /// <summary>Stops active effects matching a valid cue; invalid IDs do not mean all.</summary>
        public void StopSfx(AudioId id)
        {
            if (!id.IsValid) return;
            foreach (var voice in voices) if (voice.Cue != null && voice.Cue.Id == id) Release(voice);
        }
        private void Release(Voice voice)
        {
            var source = voice.Source;
            if (source != null)
            {
                source.Stop(); source.clip = null; source.loop = false; source.pitch = 1;
                source.spatialBlend = 0; source.volume = 0; source.mute = false;
                source.outputAudioMixerGroup = null; source.transform.localPosition = Vector3.zero;
            }
            voice.Cue = null; voice.FadeRemaining = 0; voice.Gain = 1; voice.Releasing = false;
        }
        /// <summary>Chooses an entry from the supplied list; null or empty lists do nothing.</summary>
        public AudioSource PlayRandomSfx(IReadOnlyList<AudioId> ids) =>
            ids == null || ids.Count == 0 ? null : PlaySfx(ids[UnityEngine.Random.Range(0, ids.Count)]);
        /// <summary>Changes pitch only on an active owned effect.</summary>
        public void SetPitch(AudioSource source, float pitch)
        {
            if (FindVoice(source) != null && Finite(pitch) && pitch >= 0.01f && pitch <= 3) source.pitch = pitch;
        }
        /// <summary>Returns authored seconds, independent of playback pitch; unknown IDs return zero.</summary>
        public float ClipLength(AudioId id) => Ready && id.IsValid && cues.TryGetValue(id, out var cue) && cue.Clip != null ? cue.Clip.length : 0;
        /// <summary>Starts music immediately on first play, otherwise fades out before replacing it.</summary>
        public void PlayBgm(AudioId id, float fadeOut = 0.3f)
        {
            if (!Finite(fadeOut) || fadeOut < 0 || !TryCue(id, AudioCueKind.Music, out var cue)) return;
            if (music != null && music.Id == id && (bgm.isPlaying || paused))
            {
                pendingMusic = null; musicFadeRemaining = 0; musicGain = 1; ApplySettings(); return;
            }
            if (music == null || fadeOut == 0) { StartMusic(cue); return; }
            pendingMusic = cue; musicFadeStart = musicGain;
            musicFadeDuration = musicFadeRemaining = fadeOut;
        }
        private void StartMusic(AudioCue cue)
        {
            pendingMusic = null; musicFadeRemaining = 0; musicGain = 1; paused = false; music = cue;
            bgm.Stop(); bgm.clip = cue.Clip; bgm.loop = true; bgm.pitch = 1; bgm.spatialBlend = 0;
            bgm.outputAudioMixerGroup = cue.MixerGroup; ApplySettings(); bgm.Play();
        }
        /// <summary>Pauses music and its pending replacement fade.</summary>
        public void PauseBgm() { if (Ready && music != null) { bgm.Pause(); paused = true; } }
        /// <summary>Resumes paused music without restarting its clip.</summary>
        public void UnPauseBgm() { if (Ready && paused) { bgm.UnPause(); paused = false; } }
        /// <summary>Stops music and cancels any pending replacement.</summary>
        public void StopBgm()
        {
            music = pendingMusic = null; musicFadeRemaining = 0; musicGain = 1; paused = false;
            if (bgm != null) { bgm.Stop(); bgm.clip = null; bgm.outputAudioMixerGroup = null; }
        }
        /// <summary>Borrows live product settings; resets local overrides.</summary>
        public void BindSettings(IAudioSettings value)
        {
            settings = value ?? throw new ArgumentNullException(nameof(value));
            bgmOverride = sfxOverride = null; ApplySettings();
        }
        /// <summary>Releases settings and restores unmuted defaults.</summary>
        public void UnbindSettings() { settings = null; bgmOverride = sfxOverride = null; ApplySettings(); }
        /// <summary>Overrides music mute until the next settings bind or unbind.</summary>
        public void ToggleBgmVolume(bool isMute) { bgmOverride = isMute; ApplySettings(); }
        /// <summary>Overrides SFX mute until the next settings bind or unbind.</summary>
        public void ToggleSfxVolume(bool isMute) { sfxOverride = isMute; ApplySettings(); }
        private void ApplyVoice(Voice voice)
        {
            if (voice.Source == null || voice.Cue == null) return;
            voice.Source.volume = voice.Cue.Volume * voice.Scale * voice.Gain;
            voice.Source.mute = sfxOverride ?? settings?.IsSfxMuted ?? false;
        }
        private void ApplySettings()
        {
            if (bgm != null)
            {
                bgm.mute = bgmOverride ?? settings?.IsBgmMuted ?? false;
                bgm.volume = (music?.Volume ?? 0) * musicGain;
            }
            foreach (var voice in voices) ApplyVoice(voice);
        }
        private void Update()
        {
            if (!Ready) return;
            float delta = Time.unscaledDeltaTime;
            foreach (var voice in voices)
            {
                if (voice.Cue == null) continue;
                if (voice.Source == null || (!voice.Source.loop && !voice.Source.isPlaying)) { Release(voice); continue; }
                if (voice.FadeRemaining <= 0) continue;
                voice.FadeRemaining = Mathf.Max(0, voice.FadeRemaining - delta);
                float progress = 1 - voice.FadeRemaining / voice.FadeDuration;
                voice.Gain = Mathf.Lerp(voice.FadeStart, voice.Releasing ? 0 : 1, progress);
                if (voice.Releasing && voice.FadeRemaining == 0) Release(voice);
            }
            if (!paused && pendingMusic != null)
            {
                musicFadeRemaining = Mathf.Max(0, musicFadeRemaining - delta);
                musicGain = musicFadeStart * musicFadeRemaining / musicFadeDuration;
                if (musicFadeRemaining == 0) StartMusic(pendingMusic);
            }
            ApplySettings();
        }
        /// <summary>Stops and destroys owned child sources once; leaves the caller's root intact.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; StopBgm(); StopSfx(); settings = null; bgmOverride = sfxOverride = null;
            foreach (var child in children)
            {
                if (child == null) continue;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            children.Clear(); voices.Clear(); cues = null; bgm = null;
        }
        private void OnDestroy() => Dispose();
    }
}

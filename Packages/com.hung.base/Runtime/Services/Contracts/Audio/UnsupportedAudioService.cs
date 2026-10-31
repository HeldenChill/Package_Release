using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    /// <summary>Absent Audio plugin fallback; never allocates playback resources or owns a slot.</summary>
    public sealed class UnsupportedAudioService : IAudioService
    {
        private readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);
        private void Warn(string member)
        {
            if (warned.Add(member)) Debug.LogWarning("[Unsupported] IAudioService." + member + " called; no audio plugin installed.");
        }
        /// <inheritdoc/>
        public void BindSettings(IAudioSettings settings) { if (settings == null) throw new ArgumentNullException(nameof(settings)); Warn(nameof(BindSettings)); }
        /// <inheritdoc/>
        public void UnbindSettings() => Warn(nameof(UnbindSettings));
        /// <inheritdoc/>
        public void PlayBgm(AudioId id, float fadeOut = 0.3f) => Warn(nameof(PlayBgm));
        /// <inheritdoc/>
        public void PlaySfx(AudioId id) => Warn(nameof(PlaySfx));
        /// <inheritdoc/>
        public void PlaySfx(AudioId id, Vector3 position, float volume = 1, float pitch = 1) => Warn(nameof(PlaySfx));
        /// <inheritdoc/>
        public AudioSource PlayLoopSfx(AudioId id, float fadeIn = 0.1f) { Warn(nameof(PlayLoopSfx)); return null; }
        /// <inheritdoc/>
        public void StopSfx() => Warn(nameof(StopSfx));
        /// <inheritdoc/>
        public void StopSfx(AudioId id) => Warn(nameof(StopSfx));
        /// <inheritdoc/>
        public void StopLoopSfx(AudioSource source, float fadeOut = 0.1f) => Warn(nameof(StopLoopSfx));
        /// <inheritdoc/>
        public void PlayRandomSfx(List<AudioId> ids) => Warn(nameof(PlayRandomSfx));
        /// <inheritdoc/>
        public void PauseBgm() => Warn(nameof(PauseBgm));
        /// <inheritdoc/>
        public void UnPauseBgm() => Warn(nameof(UnPauseBgm));
        /// <inheritdoc/>
        public void StopBgm() => Warn(nameof(StopBgm));
        /// <inheritdoc/>
        public void ToggleBgmVolume(bool isMute) => Warn(nameof(ToggleBgmVolume));
        /// <inheritdoc/>
        public void ToggleSfxVolume(bool isMute) => Warn(nameof(ToggleSfxVolume));
        /// <inheritdoc/>
        public void SetPitch(AudioSource source, float pitch) => Warn(nameof(SetPitch));
        /// <inheritdoc/>
        public float ClipLength(AudioId id) => 0;
    }
}

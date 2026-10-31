using System;
using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.Audio
{
    /// <summary>Publishes the playback core through the neutral Base contract.</summary>
    public sealed class AudioServiceAdapter : IAudioService
    {
        private readonly AudioPlayback core;
        /// <summary>Wraps an initialized playback core.</summary>
        public AudioServiceAdapter(AudioPlayback core) => this.core = core != null ? core : throw new ArgumentNullException(nameof(core));
        /// <inheritdoc/>
        public void BindSettings(IAudioSettings settings) => core.BindSettings(settings);
        /// <inheritdoc/>
        public void UnbindSettings() => core.UnbindSettings();
        /// <inheritdoc/>
        public void PlayBgm(AudioId id, float fadeOut = 0.3f) => core.PlayBgm(id, fadeOut);
        /// <inheritdoc/>
        public void PlaySfx(AudioId id) => core.PlaySfx(id);
        /// <inheritdoc/>
        public void PlaySfx(AudioId id, Vector3 position, float volume = 1, float pitch = 1) => core.PlaySfx(id, position, volume, pitch);
        /// <inheritdoc/>
        public AudioSource PlayLoopSfx(AudioId id, float fadeIn = 0.1f) => core.PlayLoopSfx(id, fadeIn);
        /// <inheritdoc/>
        public void StopSfx() => core.StopSfx();
        /// <inheritdoc/>
        public void StopSfx(AudioId id) => core.StopSfx(id);
        /// <inheritdoc/>
        public void StopLoopSfx(AudioSource source, float fadeOut = 0.1f) => core.StopLoopSfx(source, fadeOut);
        /// <inheritdoc/>
        public void PlayRandomSfx(List<AudioId> ids) => core.PlayRandomSfx(ids);
        /// <inheritdoc/>
        public void PauseBgm() => core.PauseBgm();
        /// <inheritdoc/>
        public void UnPauseBgm() => core.UnPauseBgm();
        /// <inheritdoc/>
        public void StopBgm() => core.StopBgm();
        /// <inheritdoc/>
        public void ToggleBgmVolume(bool isMute) => core.ToggleBgmVolume(isMute);
        /// <inheritdoc/>
        public void ToggleSfxVolume(bool isMute) => core.ToggleSfxVolume(isMute);
        /// <inheritdoc/>
        public void SetPitch(AudioSource source, float pitch) => core.SetPitch(source, pitch);
        /// <inheritdoc/>
        public float ClipLength(AudioId id) => core.ClipLength(id);
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    /// <summary>Neutral audio playback capability using product-extensible cue identities.</summary>
    public interface IAudioService
    {
        /// <summary>Borrows live settings and clears local mute overrides.</summary>
        void BindSettings(IAudioSettings settings);
        /// <summary>Releases settings and restores unmuted defaults.</summary>
        void UnbindSettings();
        /// <summary>Starts music; replacement fades out the current cue first.</summary>
        void PlayBgm(AudioId id, float fadeOut = 0.3f);
        /// <summary>Plays a 2D effect; unavailable cues do nothing.</summary>
        void PlaySfx(AudioId id);
        /// <summary>Plays an effect at a world position with gain 0..1 and pitch 0.01..3.</summary>
        void PlaySfx(AudioId id, Vector3 position, float volume = 1f, float pitch = 1f);
        /// <summary>Returns a temporary owned loop source, or null when unavailable.</summary>
        AudioSource PlayLoopSfx(AudioId id, float fadeIn = 0.1f);
        /// <summary>Stops all sound effects including loops.</summary>
        void StopSfx();
        /// <summary>Stops effects matching a valid ID; invalid does not mean all.</summary>
        void StopSfx(AudioId id);
        /// <summary>Stops an owned loop; null and foreign sources do nothing.</summary>
        void StopLoopSfx(AudioSource source, float fadeOut = 0.1f);
        /// <summary>Chooses a cue from the list; null or empty does nothing.</summary>
        void PlayRandomSfx(List<AudioId> ids);
        /// <summary>Pauses music and its replacement fade.</summary>
        void PauseBgm();
        /// <summary>Resumes paused music without restarting the clip.</summary>
        void UnPauseBgm();
        /// <summary>Stops music and cancels pending replacement.</summary>
        void StopBgm();
        /// <summary>Overrides music mute until settings rebind/unbind.</summary>
        void ToggleBgmVolume(bool isMute);
        /// <summary>Overrides effect mute until settings rebind/unbind.</summary>
        void ToggleSfxVolume(bool isMute);
        /// <summary>Changes pitch on an active owned effect only.</summary>
        void SetPitch(AudioSource source, float pitch);
        /// <summary>Returns authored duration in seconds; unknown IDs return zero.</summary>
        float ClipLength(AudioId id);
    }
}

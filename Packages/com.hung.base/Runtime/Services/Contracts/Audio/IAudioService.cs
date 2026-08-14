using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    public interface IAudioService
    {
        public void PlayBgm(BGM_TYPE type, float fadeOut = 0.3f);
        public void PlaySfx(SFX_TYPE type);
        public AudioSource PlayLoopSfx(SFX_TYPE type, float fadeIn = 0.1f);
        public void StopSfx(SFX_TYPE type = SFX_TYPE.NONE);
        public void StopLoopSfx(AudioSource source, float fadeOut = 0.1f);
        public void PlayRandomSfx(List<SFX_TYPE> sfxTypes);
        public void PauseBgm();
        public void UnPauseBgm();
        public void StopBgm();
        public void ToggleBgmVolume(bool isMute);
        public void ToggleSfxVolume(bool isMute);
    }
}

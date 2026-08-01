using Hung.DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hung.Utilities.Timer;

namespace Hung.Utilities
{
    public class VFXObject : GameUnit
    {
        [SerializeField]
        VFXColor vfxColor;
        [SerializeField]
        ParticleSystem mainPs;

        public ParticleSystem MainPs => mainPs;
        public VFXColor VfxColor => vfxColor;

        public void Play()
        {
            gameObject.SetActive(true);
            mainPs.Play();
            TimerManager.Ins.WaitForTime(mainPs.main.duration, () => DespawnVFX(this));
        }
        private void DespawnVFX(VFXObject vfx)
        {
            vfx.gameObject.SetActive(false);
            SimplePool.Despawn(vfx);
        }
    }
}

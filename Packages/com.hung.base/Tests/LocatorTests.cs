using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Hung.Base;

namespace Hung.Base.Tests
{
    // Locator (Runtime/Services/Locator/Locator.cs) has NO generic Register<T>/Resolve<T> -- it's ~14
    // typed static properties, one per domain, plain get/set. Reading an unregistered slot returns null
    // (no exception); only Locator.Data has a ResetForTests-style hook (com.hung.data's ResetDataForTests,
    // added Ph5) -- every other domain, including Audio used here, has none, so tests must null the slot
    // themselves in teardown to avoid cross-test static-state pollution (documented gap, not fixed here).
    public class LocatorTests
    {
        private class FakeAudioService : IAudioService
        {
            public void PlayBgm(BGM_TYPE type, float fadeOut = 0.3f) { }
            public void PlaySfx(SFX_TYPE type) { }
            public AudioSource PlayLoopSfx(SFX_TYPE type, float fadeIn = 0.1f) => null;
            public void StopSfx(SFX_TYPE type = SFX_TYPE.NONE) { }
            public void StopLoopSfx(AudioSource source, float fadeOut = 0.1f) { }
            public void PlayRandomSfx(List<SFX_TYPE> sfxTypes) { }
            public void PauseBgm() { }
            public void UnPauseBgm() { }
            public void StopBgm() { }
            public void ToggleBgmVolume(bool isMute) { }
            public void ToggleSfxVolume(bool isMute) { }
        }

        [TearDown]
        public void TearDown()
        {
            Locator.Audio = null;
        }

        [Test]
        public void Register_Resolve_Roundtrip()
        {
            var fake = new FakeAudioService();

            Locator.Audio = fake;

            Assert.AreSame(fake, Locator.Audio);
        }

        [Test]
        public void Resolve_Unregistered_ReturnsNull()
        {
            Locator.Audio = null;

            Assert.IsNull(Locator.Audio);
        }
    }
}

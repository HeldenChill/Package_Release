using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Hung.Base;

namespace Hung.Base.Tests
{
    // Typed accessors resolve explicit registry ownership.
    public class LocatorTests
    {
        [Test]
        public void NoGameplaySlotOrPublicSetters()
        {
            Assert.IsNull(typeof(Locator).GetProperty("Gameplay"));
            foreach (var property in typeof(Locator).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                Assert.IsNull(property.GetSetMethod(), property.Name);
        }

        private class FakeAudioService : IAudioService
        {
            public void PlayBgm(AudioId type, float fadeOut = 0.3f) { }
            public void PlaySfx(AudioId type) { }
            public AudioSource PlayLoopSfx(AudioId type, float fadeIn = 0.1f) => null;
            public void StopSfx() { }
            public void StopSfx(AudioId id) { }
            public void StopLoopSfx(AudioSource source, float fadeOut = 0.1f) { }
            public void PlayRandomSfx(List<AudioId> sfxTypes) { }
            public void PauseBgm() { }
            public void UnPauseBgm() { }
            public void StopBgm() { }
            public void ToggleBgmVolume(bool isMute) { }
            public void ToggleSfxVolume(bool isMute) { }
            public void BindSettings(IAudioSettings settings) { }
            public void UnbindSettings() { }
            public void PlaySfx(AudioId id, Vector3 position, float volume = 1, float pitch = 1) { }
            public void SetPitch(AudioSource source, float pitch) { }
            public float ClipLength(AudioId id) => 0;
        }

        [TearDown]
        public void TearDown()
        {
            Hung.Base.ServiceSlots.Unregister<IAudioService>(Hung.Base.ServiceSlots.Get<IAudioService>());
        }

        [Test]
        public void Register_Resolve_Roundtrip()
        {
            var fake = new FakeAudioService();

            Hung.Base.ServiceSlots.Unregister<IAudioService>(Hung.Base.ServiceSlots.Get<IAudioService>()); Hung.Base.ServiceSlots.Register<IAudioService>(fake);

            Assert.AreSame(fake, Locator.Audio);
        }

        [Test]
        public void Resolve_Unregistered_ReturnsAudioFallback()
        {
            Hung.Base.ServiceSlots.Unregister<IAudioService>(Hung.Base.ServiceSlots.Get<IAudioService>());

            Assert.IsInstanceOf<UnsupportedAudioService>(Locator.Audio);
        }
    }
}

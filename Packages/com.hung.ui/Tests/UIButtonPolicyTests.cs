using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.Tests
{
    public sealed class MaskSeamProbe : MaskRaycastImage
    {
        public void SetTargets(List<RectTransform> targets) => maskRectTfs = targets;
        public int Count => maskRectTfs?.Count ?? 0;
    }

    public sealed class UIButtonPolicyTests
    {
        private sealed class CountingAudio : IAudioService
        {
            public int Sfx;
            public void PlayBgm(AudioId type, float fadeOut = 0.3f) { }
            public void PlaySfx(AudioId type) => Sfx++;
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

        private GameObject gameObject;
        private UIButton button;
        private CountingAudio audio;
        private int clicks;

        [SetUp]
        public void SetUp()
        {
            ServiceSlots.ResetForTests();
            gameObject = new GameObject("Button", typeof(RectTransform));
            button = gameObject.AddComponent<UIButton>();
            audio = new CountingAudio();
            Hung.Base.ServiceSlots.Unregister<IAudioService>(Hung.Base.ServiceSlots.Get<IAudioService>()); Hung.Base.ServiceSlots.Register<IAudioService>(audio);
            clicks = 0;
            button._OnClick = _ => clicks++;
        }

        [TearDown]
        public void TearDown()
        {
            UIButton.DefaultClickSfx = true;
            Hung.Base.ServiceSlots.Unregister<IAudioService>(Hung.Base.ServiceSlots.Get<IAudioService>());
            Object.DestroyImmediate(gameObject);
        }

        private void Click() =>
            typeof(UIButton).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, null);

        [Test]
        public void Default_click_plays_sfx_and_raises_OnClick()
        {
            Click();
            Assert.AreEqual(1, audio.Sfx);
            Assert.AreEqual(1, clicks);
        }

        [Test]
        public void Migrated_none_stays_silent_while_new_button_defaults_to_click()
        {
            typeof(UIButton).GetField("audioId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(button, default(AudioId));
            Click();
            Assert.That(audio.Sfx, Is.Zero);
            Assert.That(clicks, Is.EqualTo(1));
        }

        [Test]
        public void Silent_policy_raises_OnClick_without_sfx()
        {
            UIButton.DefaultClickSfx = false;
            Click();
            Assert.AreEqual(0, audio.Sfx);
            Assert.AreEqual(1, clicks);
        }

        [Test]
        public void Click_without_audio_service_still_raises_OnClick()
        {
            Hung.Base.ServiceSlots.Unregister<IAudioService>(Hung.Base.ServiceSlots.Get<IAudioService>());
            LogAssert.Expect(LogType.Warning, "[Unsupported] IAudioService.PlaySfx called; no audio plugin installed.");
            Assert.DoesNotThrow(Click);
            Assert.AreEqual(1, clicks);
        }

        [Test]
        public void Tf_exposes_serialized_transform_as_rect()
        {
            typeof(UIButton).GetField("tf", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(button, gameObject.transform);
            Assert.AreSame(gameObject.transform, button.Tf);
            Assert.AreSame(gameObject.GetComponent<RectTransform>(), button.RectTf);
        }

        [Test]
        public void Mask_list_is_settable_by_subclass()
        {
            var maskObject = new GameObject("Mask", typeof(RectTransform));
            try
            {
                var probe = maskObject.AddComponent<MaskSeamProbe>();
                probe.SetTargets(new List<RectTransform> { maskObject.GetComponent<RectTransform>() });
                Assert.AreEqual(1, probe.Count);
            }
            finally { Object.DestroyImmediate(maskObject); }
        }
    }
}

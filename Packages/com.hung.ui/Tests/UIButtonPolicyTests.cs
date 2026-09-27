using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;

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
            public void PlayBgm(BGM_TYPE type, float fadeOut = 0.3f) { }
            public void PlaySfx(SFX_TYPE type) => Sfx++;
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

        private GameObject gameObject;
        private UIButton button;
        private CountingAudio audio;
        private int clicks;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Button", typeof(RectTransform));
            button = gameObject.AddComponent<UIButton>();
            audio = new CountingAudio();
            Locator.Audio = audio;
            clicks = 0;
            button._OnClick = _ => clicks++;
        }

        [TearDown]
        public void TearDown()
        {
            UIButton.DefaultClickSfx = true;
            Locator.Audio = null;
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
            Locator.Audio = null;
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

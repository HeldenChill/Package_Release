using System;
using System.Collections.Generic;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.UI.Tests
{
    public sealed class LifecycleProbeAnim : UIAnim
    {
        public readonly List<ANIM> Calls = new List<ANIM>();
        public Propertys[] Configured;

        public override IReadOnlyList<Propertys> Datas => Configured;
        public override void Stop() { }

        public override void Play(ANIM anim)
        {
            Calls.Add(anim);
            bool configured = false;
            foreach (Propertys property in Configured)
                if (property.Id == anim) configured = true;
            if (!configured || !TryBeginOrQueue(anim)) return;
            state = anim;
        }

        public void Complete(ANIM anim) => OnAnimExit((int)anim);
    }

    public sealed class LifecycleProbeCanvas : UISCanvas
    {
        public int Closed;

        public void Configure(UIAnim[] effects, bool setActiveByAnim, Button legacy = null, List<Button> additional = null)
        {
            anims = effects;
            canvasComponent = Array.Empty<UICanvasComponent>();
            data = new Propertys { SetActiveByAnim = setActiveByAnim };
            closeButton = legacy;
            closeBtns = additional;
            Start();
        }

        public void SetActiveByAnim(bool enabled) => data.SetActiveByAnim = enabled;

        protected override void OnClose()
        {
            base.OnClose();
            Closed++;
        }
    }

    public sealed class CustomTransitionProbe : UITransition
    {
        public int IntroCount;
        public override void PlayIntro(Action onComplete)
        {
            IntroCount++;
            onComplete?.Invoke();
        }
        public override void PlayOutro(Action onComplete) => onComplete?.Invoke();
        public override void Interrupt() { }
    }

    public sealed class NoOpProbeAnim : UIAnim
    {
        public Propertys[] Configured;
        public override IReadOnlyList<Propertys> Datas => Configured;
        public override void Play(ANIM anim) { }
        public override void Stop() { }
    }

    public sealed class UISCanvasLifecycleTests
    {
        private sealed class UiServiceStub : IUIService
        {
            public UIBackStack BackStack { get; } = new UIBackStack();
            public RectTransform ParentCanvasTf => null;
            public Canvas Canvas => null;
            public CanvasScaler CanvasScaler => null;
            public void SetCameraScreenSpace(Camera cam) { }
            public T OpenUI<T>(object param = null) where T : UICanvas => null;
            public UICanvas OpenUIDirectly(UICanvas prefab, object param = null) => null;
            public void CloseUI<T>() where T : UICanvas { }
            public void ShowUI<T>() where T : UICanvas { }
            public void HideUI<T>() where T : UICanvas { }
            public bool IsOpened<T>() where T : UICanvas => false;
            public bool IsLoaded<T>() where T : UICanvas => false;
            public T GetUI<T>() where T : UICanvas => null;
            public void GetUIAsync<T>(Action<T> onComplete) where T : UICanvas { }
            public void PreloadUI<T>() where T : UICanvas { }
            public void UpdateAllUI() { }
            public void DestroyAllUI(HashSet<UICanvas> exception) { }
            public void HideAll() { }
            public void CloseAll() { }
        }

        private GameObject gameObject;
        private LifecycleProbeCanvas canvas;
        private LifecycleProbeAnim first;
        private LifecycleProbeAnim second;
        private UiServiceStub service;
        private Button legacyButton;
        private Button extraButton;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("LifecycleProbeCanvas");
            canvas = gameObject.AddComponent<LifecycleProbeCanvas>();
            first = gameObject.AddComponent<LifecycleProbeAnim>();
            second = gameObject.AddComponent<LifecycleProbeAnim>();
            first.Configured = new[] { new UIAnim.Propertys { Id = UIAnim.ANIM.SHOW }, new UIAnim.Propertys { Id = UIAnim.ANIM.HIDE } };
            second.Configured = new[] { new UIAnim.Propertys { Id = UIAnim.ANIM.SHOW }, new UIAnim.Propertys { Id = UIAnim.ANIM.HIDE } };
            legacyButton = new GameObject("LegacyClose").AddComponent<Button>();
            legacyButton.transform.SetParent(gameObject.transform);
            extraButton = new GameObject("ExtraClose").AddComponent<Button>();
            extraButton.transform.SetParent(gameObject.transform);
            canvas.Configure(new UIAnim[] { first, second }, true, legacyButton,
                new List<Button> { extraButton, legacyButton, extraButton });
            service = new UiServiceStub();
            Locator.UI = service;
        }

        [TearDown]
        public void TearDown()
        {
            Locator.UI = null;
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Root_animation_does_not_bypass_composite_or_play_show_twice()
        {
            canvas.Open();

            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW }, first.Calls);
            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW }, second.Calls);
        }

        [Test]
        public void Authored_non_animation_transition_remains_selected()
        {
            var custom = gameObject.AddComponent<CustomTransitionProbe>();

            canvas.Open();

            Assert.AreEqual(1, custom.IntroCount);
        }

        [Test]
        public void Close_waits_for_both_hide_effects()
        {
            canvas.Open();
            first.Complete(UIAnim.ANIM.SHOW);
            second.Complete(UIAnim.ANIM.SHOW);

            canvas.Close();
            Assert.Contains(UIAnim.ANIM.HIDE, first.Calls);
            Assert.Contains(UIAnim.ANIM.HIDE, second.Calls);

            first.Complete(UIAnim.ANIM.HIDE);
            Assert.AreEqual(0, canvas.Closed);
            second.Complete(UIAnim.ANIM.HIDE);
            Assert.AreEqual(1, canvas.Closed);
        }

        [Test]
        public void Close_without_hide_effect_completes_immediately()
        {
            first.Configured = new[] { new UIAnim.Propertys { Id = UIAnim.ANIM.SHOW } };
            second.Configured = new[] { new UIAnim.Propertys { Id = UIAnim.ANIM.SHOW } };
            canvas.Open();
            canvas.Close();

            Assert.AreEqual(1, canvas.Closed);
        }

        [Test]
        public void Configured_effect_that_cannot_start_does_not_hold_show_or_close()
        {
            NoOpProbeAnim noOp = gameObject.AddComponent<NoOpProbeAnim>();
            noOp.Configured = new[]
            {
                new UIAnim.Propertys { Id = UIAnim.ANIM.SHOW },
                new UIAnim.Propertys { Id = UIAnim.ANIM.HIDE }
            };
            canvas.Configure(new UIAnim[] { noOp }, true);
            canvas.Open();
            Assert.IsTrue(canvas.IsFullyShown);

            canvas.Close();
            Assert.AreEqual(1, canvas.Closed);
        }

        [Test]
        public void Close_completes_when_active_toggle_is_disabled()
        {
            canvas.SetActiveByAnim(false);
            canvas.Open();
            first.Complete(UIAnim.ANIM.SHOW);
            second.Complete(UIAnim.ANIM.SHOW);
            canvas.Close();
            first.Complete(UIAnim.ANIM.HIDE);
            second.Complete(UIAnim.ANIM.HIDE);

            Assert.AreEqual(1, canvas.Closed);
        }

        [Test]
        public void Fully_shown_waits_for_every_show_effect_and_resets_on_hide()
        {
            int events = 0;
            canvas.FullyShown += () => events++;
            canvas.Open();
            Assert.IsFalse(canvas.IsFullyShown);

            first.Complete(UIAnim.ANIM.SHOW);
            Assert.IsFalse(canvas.IsFullyShown);
            second.Complete(UIAnim.ANIM.SHOW);
            Assert.IsTrue(canvas.IsFullyShown);
            Assert.AreEqual(1, events);

            canvas.Hide();
            Assert.IsFalse(canvas.IsFullyShown);
        }

        [Test]
        public void Fully_shown_is_immediate_without_show_effects()
        {
            first.Configured = new[] { new UIAnim.Propertys { Id = UIAnim.ANIM.HIDE } };
            second.Configured = new[] { new UIAnim.Propertys { Id = UIAnim.ANIM.HIDE } };
            int events = 0;
            canvas.FullyShown += () => events++;

            canvas.Open();

            Assert.IsTrue(canvas.IsFullyShown);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void Composite_intro_callback_waits_for_all_show_effects()
        {
            canvas.Open();
            var transition = gameObject.GetComponent<UISCanvasTransition>();
            int completions = 0;
            transition.PlayIntro(() => completions++);
            first.Complete(UIAnim.ANIM.SHOW);
            Assert.AreEqual(0, completions);
            second.Complete(UIAnim.ANIM.SHOW);
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void Interrupted_intro_callback_does_not_complete_after_reopen()
        {
            canvas.Open();
            var transition = gameObject.GetComponent<UISCanvasTransition>();
            int staleCompletions = 0;
            transition.PlayIntro(() => staleCompletions++);
            canvas.Open();
            first.Complete(UIAnim.ANIM.SHOW);
            second.Complete(UIAnim.ANIM.SHOW);
            Assert.AreEqual(0, staleCompletions);
        }

        [Test]
        public void Legacy_and_additional_close_buttons_are_both_exposed_and_active()
        {
            Assert.AreEqual(2, canvas.CloseButtons.Count);
            canvas.Open();
            first.Complete(UIAnim.ANIM.SHOW);
            second.Complete(UIAnim.ANIM.SHOW);

            extraButton.onClick.Invoke();
            first.Complete(UIAnim.ANIM.HIDE);
            second.Complete(UIAnim.ANIM.HIDE);

            Assert.AreEqual(1, canvas.Closed);
        }

        [Test]
        public void Hiding_releases_authored_raycast_blocker_and_reopen_restores_it()
        {
            CanvasGroup blocker = gameObject.AddComponent<CanvasGroup>();
            blocker.blocksRaycasts = true;
            GameObject decoration = new GameObject("NonBlocking", typeof(CanvasGroup));
            decoration.transform.SetParent(gameObject.transform);
            CanvasGroup nonBlocking = decoration.GetComponent<CanvasGroup>();
            nonBlocking.blocksRaycasts = false;

            canvas.Open();
            Assert.IsTrue(blocker.blocksRaycasts);
            Assert.IsFalse(nonBlocking.blocksRaycasts);
            canvas.Hide();
            Assert.IsFalse(blocker.blocksRaycasts);
            Assert.IsFalse(nonBlocking.blocksRaycasts);

            canvas.Open();
            Assert.IsTrue(blocker.blocksRaycasts);
            Assert.IsFalse(nonBlocking.blocksRaycasts);
        }
    }
}

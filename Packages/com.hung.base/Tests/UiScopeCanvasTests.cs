using System;
using System.Collections.Generic;
using Hung.UI;
using Hung.UI.Scoping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.Base.Tests
{
    public sealed class ScopeCanvasProbe : UICanvas
    {
        public UiScopeRegistry Registry;
        public UiScope DeclaredScope;
        public int Closed;

        public override UiScope Scope => DeclaredScope;
        protected override UiScopeRegistry ScopeRegistry => Registry;
        protected override void OnClose() => Closed++;
    }

    public sealed class ScopeTransitionProbe : UITransition
    {
        public int OutroCount;
        public Action PendingOutro;

        public override void PlayIntro(Action onComplete) => onComplete?.Invoke();
        public override void PlayOutro(Action onComplete)
        {
            OutroCount++;
            PendingOutro = onComplete;
        }

        public override void Interrupt() { }
    }

    public sealed class UiScopeCanvasTests
    {
        private sealed class FakeUiService : IUIService
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
        private UiScopeRegistry registry;
        private ScopeCanvasProbe canvas;
        private ScopeTransitionProbe transition;
        private FakeUiService service;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Scoped canvas");
            registry = new UiScopeRegistry();
            canvas = gameObject.AddComponent<ScopeCanvasProbe>();
            transition = gameObject.AddComponent<ScopeTransitionProbe>();
            canvas.Registry = registry;
            canvas.DeclaredScope = UiScope.At(UiScopePath.Of("Home"));
            service = new FakeUiService();
            Locator.UI = service;
        }

        [TearDown]
        public void TearDown()
        {
            Locator.UI = null;
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Scope_exit_starts_one_close_even_before_outro_finishes()
        {
            registry.Enter(UiScopePath.Of("Home/Shop"));
            canvas.Open();

            registry.Enter(UiScopePath.Of("Gameplay"));
            registry.Enter(UiScopePath.Of("Home"));

            Assert.AreEqual(1, transition.OutroCount);
            Assert.AreEqual(0, canvas.Closed);

            transition.PendingOutro();
            Assert.AreEqual(1, canvas.Closed);
            Assert.IsFalse(gameObject.activeSelf);
            Assert.IsNull(service.BackStack.Top);
        }

        [Test]
        public void Reopen_ignores_stale_outro_completion()
        {
            registry.Enter(UiScopePath.Of("Home/Shop"));
            canvas.Open();
            canvas.Close();
            Action staleOutro = transition.PendingOutro;

            registry.Enter(UiScopePath.Of("Home/Pet"));
            canvas.Open();
            staleOutro();

            Assert.IsTrue(gameObject.activeSelf);
            Assert.AreEqual(0, canvas.Closed);
            Assert.AreSame(canvas, service.BackStack.Top);
        }

        [Test]
        public void Destroyed_canvas_is_removed_from_scope_registry()
        {
            registry.Enter(UiScopePath.Of("Home"));
            canvas.Open();
            typeof(UICanvas).GetMethod("OnDestroy",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(canvas, null);
            UnityEngine.Object.DestroyImmediate(gameObject);

            var bound = typeof(UiScopeRegistry).GetField("_bound",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual(0, ((System.Collections.IDictionary)bound.GetValue(registry)).Count);
        }
    }
}

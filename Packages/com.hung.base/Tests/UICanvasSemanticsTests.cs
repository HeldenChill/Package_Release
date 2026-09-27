using System;
using System.Collections.Generic;
using Hung.UI;
using Hung.UI.Scoping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Hung.Base.Tests
{
    /// <summary>Minimal IUIService for canvas lifecycle tests: only BackStack is real.</summary>
    public sealed class TestUiService : IUIService
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
        public void CloseUIDirectly(UICanvas canvas) { if (canvas != null) canvas.CloseDirectly(); }
        public bool IsContain(UICanvas canvas) => false;
        public bool IsInBackStack(UICanvas canvas) => BackStack.Contains(canvas);
        public void PreloadUI<T>() where T : UICanvas { }
        public void UpdateAllUI() { }
        public void DestroyAllUI(HashSet<UICanvas> exception) { }
        public void HideAll() { }
        public void CloseAll() { }
    }

    public sealed class SemanticsCanvasProbe : UICanvas
    {
        public UiScopeRegistry Registry = new UiScopeRegistry();
        public UiScope DeclaredScope = UiScope.Global;
        public int OpenCalls;
        public int CloseCalls;
        public int Closed;
        public int Backs;

        public override UiScope Scope => DeclaredScope;
        protected override UiScopeRegistry ScopeRegistry => Registry;

        public override void Open(object param = null)
        {
            OpenCalls++;
            base.Open(param);
        }

        public override void Close()
        {
            CloseCalls++;
            base.Close();
        }

        protected override void OnClose() => Closed++;
        protected override void OnBackKey() => Backs++;
    }

    public sealed class UICanvasSemanticsTests
    {
        private GameObject gameObject;
        private SemanticsCanvasProbe canvas;
        private TestUiService service;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Semantics canvas");
            canvas = gameObject.AddComponent<SemanticsCanvasProbe>();
            service = new TestUiService();
            Locator.UI = service;
        }

        [TearDown]
        public void TearDown()
        {
            UICanvas.BackPushPolicy = UIBackPushPolicy.OnOpen;
            Locator.UI = null;
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Open_override_runs_once_and_activates()
        {
            gameObject.SetActive(false);
            canvas.Open();

            Assert.AreEqual(1, canvas.OpenCalls);
            Assert.IsTrue(gameObject.activeSelf);
        }

        [Test]
        public void Scope_exit_routes_through_the_Close_override()
        {
            canvas.DeclaredScope = UiScope.At(UiScopePath.Of("Home"));
            canvas.Registry.Enter(UiScopePath.Of("Home/Shop"));
            canvas.Open();

            canvas.Registry.Enter(UiScopePath.Of("Gameplay"));

            Assert.AreEqual(1, canvas.CloseCalls);
            Assert.AreEqual(1, canvas.Closed);
            Assert.IsFalse(gameObject.activeSelf);
        }

        [Test]
        public void Default_policy_pushes_on_open()
        {
            canvas.Open();

            Assert.AreSame(canvas, service.BackStack.Top);
        }

        [Test]
        public void Explicit_setup_policy_pushes_only_on_setup()
        {
            UICanvas.BackPushPolicy = UIBackPushPolicy.ExplicitSetup;

            canvas.Open();
            Assert.IsNull(service.BackStack.Top);

            canvas.Setup();
            Assert.AreSame(canvas, service.BackStack.Top);
            service.BackStack.InvokeBack();
            Assert.AreEqual(1, canvas.Backs);
        }

        [Test]
        public void CloseDirectly_without_open_deactivates_and_skips_OnClose()
        {
            canvas.CloseDirectly();

            Assert.IsFalse(gameObject.activeSelf);
            Assert.AreEqual(0, canvas.Closed, "PVM CloseDirectly never raised the close hook.");
            Assert.IsNull(service.BackStack.Top);
        }

        [Test]
        public void CloseDirectly_during_outro_then_reopen_ignores_stale_outro()
        {
            var transition = gameObject.AddComponent<ScopeTransitionProbe>();
            canvas.Open();
            canvas.Close();
            Action staleOutro = transition.PendingOutro;

            canvas.CloseDirectly();
            canvas.Open();
            staleOutro();

            Assert.IsTrue(gameObject.activeSelf);
            Assert.AreEqual(0, canvas.Closed);
            Assert.AreSame(canvas, service.BackStack.Top);
        }

        [Test]
        public void Lifecycle_calls_do_not_throw_without_ui_service()
        {
            Locator.UI = null;

            Assert.DoesNotThrow(() => canvas.Open());
            Assert.DoesNotThrow(() => canvas.Setup());
            Assert.DoesNotThrow(() => canvas.CloseDirectly());
            Assert.IsFalse(gameObject.activeSelf);
        }
    }
}

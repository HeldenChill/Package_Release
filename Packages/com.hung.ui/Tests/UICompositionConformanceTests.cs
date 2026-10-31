using System;
using System.Collections.Generic;
using System.Reflection;
using Hung.Base;
using Hung.UI.Scoping;
using NUnit.Framework;
using UnityEngine;

namespace Hung.UI.Tests
{
    public sealed class CompositionCanvas : UICanvas
    {
        private readonly UiScopeRegistry scope = new UiScopeRegistry();
        protected override UiScopeRegistry ScopeRegistry => scope;
        public int Setups, Opens, Backs;
        public object LastParameter;
        public override void Setup(object param = null) { Setups++; base.Setup(param); }
        protected override void OnOpen(object param) { Opens++; LastParameter = param; }
        protected override void OnBackKey() { Backs++; Close(); }
    }

    // Fictional product adapter only: proves interface and concrete dispatch without migrating assets.
    public sealed class CompositionManager : UIManager, IUIService
    {
        public new T OpenUI<T>(object param = null) where T : UICanvas
        {
            T instance = GetUI<T>();
            instance.Setup(param);
            instance.Open(param);
            return instance;
        }
    }

    public sealed class UICompositionConformanceTests
    {
        private readonly List<GameObject> owned = new List<GameObject>();
        private IUIService savedService;
        private UIBackPushPolicy savedPolicy;

        [SetUp]
        public void SetUp()
        {
            savedService = ServiceSlots.Get<IUIService>();
            if (savedService != null) ServiceSlots.Unregister(savedService);
            savedPolicy = UICanvas.BackPushPolicy;
            UICanvas.BackPushPolicy = UIBackPushPolicy.OnOpen;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
            owned.Clear();
            ServiceSlots.Unregister(ServiceSlots.Get<IUIService>());
            if (savedService != null) ServiceSlots.Register(savedService);
            UICanvas.BackPushPolicy = savedPolicy;
        }

        private T Manager<T>() where T : UIManager
        {
            var root = new GameObject("UI-C0 composition manager");
            owned.Add(root);
            T manager = root.AddComponent<T>();
            // EditMode does not guarantee normal MonoBehaviour Awake. Initialize through a public lazy seam.
            _ = manager.BackStack;
            ServiceSlots.Register<IUIService>(manager);
            return manager;
        }

        private CompositionCanvas Seed(UIManager manager)
        {
            var prefab = new GameObject("UI-C0 composition prefab", typeof(RectTransform), typeof(CompositionCanvas));
            owned.Add(prefab);
            prefab.SetActive(false);
            var registry = (CanvasRegistry)typeof(UIManager).GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            var instance = registry.RegisterPrefab(prefab.GetComponent<CompositionCanvas>());
            owned.Add(instance.gameObject);
            return instance;
        }

        private static IUIService RequireUi()
        {
            if (!ServiceSlots.TryGet<IUIService>(out var ui))
                throw new InvalidOperationException("UI-C0 required UI owner is not composed.");
            return ui;
        }

        [Test]
        public void Required_ui_absence_is_explicit_and_optional_ui_is_skipped()
        {
            Assert.IsNull(Locator.UI, "Approved UI absence exception; do not install a fake fallback.");
            StringAssert.Contains("required UI owner", Assert.Throws<InvalidOperationException>(() => RequireUi()).Message);
            int actions = 0;
            if (ServiceSlots.TryGet<IUIService>(out var ui)) { actions++; _ = ui.BackStack; }
            Assert.AreEqual(0, actions);
        }

        [Test]
        public void Package_interface_open_pushes_once_and_preserves_parameter()
        {
            UIManager manager = Manager<UIManager>();
            CompositionCanvas canvas = Seed(manager);
            object parameter = new object();
            IUIService service = manager;
            Assert.AreSame(manager, RequireUi());
            Assert.AreSame(manager, Locator.UI);
            service.OpenUI<CompositionCanvas>(parameter);
            service.OpenUI<CompositionCanvas>(parameter);
            Assert.AreEqual(2, canvas.Opens);
            Assert.AreEqual(0, canvas.Setups);
            Assert.AreSame(parameter, canvas.LastParameter);
            Assert.AreEqual(1, manager.BackStack.Canvases.Count);
        }

        [Test]
        public void Explicit_setup_adapter_dispatches_through_interface_and_concrete_type()
        {
            UICanvas.BackPushPolicy = UIBackPushPolicy.ExplicitSetup;
            CompositionManager manager = Manager<CompositionManager>();
            CompositionCanvas canvas = Seed(manager);
            ((IUIService)manager).OpenUI<CompositionCanvas>("interface");
            manager.OpenUI<CompositionCanvas>("concrete");
            Assert.AreEqual(2, canvas.Setups);
            Assert.AreEqual(2, canvas.Opens);
            Assert.AreEqual("concrete", canvas.LastParameter);
            Assert.AreEqual(1, manager.BackStack.Canvases.Count);
        }

        [Test]
        public void Package_typed_reference_does_not_implicitly_run_product_setup()
        {
            UICanvas.BackPushPolicy = UIBackPushPolicy.ExplicitSetup;
            CompositionManager adapter = Manager<CompositionManager>();
            CompositionCanvas canvas = Seed(adapter);
            UIManager packageTyped = adapter;
            packageTyped.OpenUI<CompositionCanvas>();
            Assert.AreEqual(0, canvas.Setups);
            Assert.AreEqual(0, adapter.BackStack.Canvases.Count,
                "A consumer census must remove/reroute this dispatch path when ExplicitSetup is selected.");
        }

        [Test]
        public void Hide_preserves_stack_close_removes_it_and_back_order_is_lifo()
        {
            UIManager manager = Manager<UIManager>();
            CompositionCanvas first = Seed(manager);
            var secondRoot = new GameObject("UI-C0 direct second", typeof(RectTransform), typeof(CompositionCanvas));
            owned.Add(secondRoot);
            var second = secondRoot.GetComponent<CompositionCanvas>();
            first.Open();
            second.Open();
            second.Hide();
            Assert.IsTrue(manager.BackStack.Contains(second));
            second.Show();
            manager.BackStack.InvokeBack();
            Assert.AreEqual(1, second.Backs);
            Assert.AreSame(first, manager.BackStack.Top);
            manager.BackStack.InvokeBack();
            Assert.AreEqual(1, first.Backs);
            Assert.IsNull(manager.BackStack.Top);
        }
    }
}

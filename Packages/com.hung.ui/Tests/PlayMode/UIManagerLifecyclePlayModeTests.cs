using System;
using System.Collections;
using System.Threading.Tasks;
using Hung.Base;
using Hung.UI.Scoping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.PlayModeTests
{
    public sealed class PlayModeProbeCanvas : UICanvas { }
    public sealed class PlayModeScopedCanvas : UICanvas
    {
        public UiScopeRegistry Registry;
        public override UiScope Scope => UiScope.At(UiScopePath.Of("Home"));
        protected override UiScopeRegistry ScopeRegistry => Registry;
    }

    public sealed class UIManagerLifecyclePlayModeTests
    {
        private sealed class PrefabProvider : IUIAsyncPrefabProvider
        {
            public GameObject Prefab;
            public int Releases;

            public Task<UIPrefabLease> LoadAsync(string address) =>
                Task.FromResult(new UIPrefabLease(Prefab, () => Releases++));
        }

        [UnityTest]
        public IEnumerator Addressed_canvas_opens_closes_and_manager_destroy_releases_prefab()
        {
            GameObject prefab = new GameObject("UI Probe Prefab", typeof(RectTransform), typeof(PlayModeProbeCanvas));
            GameObject managerObject = new GameObject("UI Manager Probe", typeof(UIManager));
            var provider = new PrefabProvider { Prefab = prefab };
            PlayModeProbeCanvas canvas = null;
            try
            {
                UIManager manager = managerObject.GetComponent<UIManager>();
                manager.SetAsyncProvider(provider);
                Task<UIAcquireResult<PlayModeProbeCanvas>> pending =
                    manager.AcquireAsync<PlayModeProbeCanvas>("ui/probe");
                while (!pending.IsCompleted) yield return null;
                Assert.IsTrue(pending.Result.Succeeded, pending.Result.Diagnostic);
                canvas = pending.Result.Canvas;

                canvas.Open();
                Assert.IsTrue(canvas.gameObject.activeSelf);
                canvas.Close();
                Assert.IsFalse(canvas.gameObject.activeSelf);

                UnityEngine.Object.Destroy(managerObject);
                yield return null;
                Assert.AreEqual(1, provider.Releases);
            }
            finally
            {
                Locator.UI = null;
                if (canvas != null) UnityEngine.Object.Destroy(canvas.gameObject);
                if (managerObject != null) UnityEngine.Object.Destroy(managerObject);
                UnityEngine.Object.Destroy(prefab);
            }
        }

        [UnityTest]
        public IEnumerator Existing_no_wifi_popup_opens_and_closes_without_missing_components()
        {
            GameObject prefab = Resources.Load<GameObject>("UI/NoWifiPopup");
            Assert.IsNotNull(prefab);
            GameObject managerObject = new GameObject("UI Manager Prefab Smoke", typeof(UIManager));
            GameObject popup = null;
            try
            {
                popup = UnityEngine.Object.Instantiate(prefab);
                UISCanvas canvas = popup.GetComponent<UISCanvas>();
                Assert.IsNotNull(canvas);
                canvas.Open();
                yield return new WaitForSeconds(1f);
                Assert.IsTrue(popup.activeSelf);

                canvas.Close();
                yield return new WaitForSeconds(1f);
                Assert.IsTrue(popup == null || !popup.activeSelf,
                    "Popup should be destroyed or inactive after close.");
            }
            finally
            {
                Locator.UI = null;
                if (popup != null) UnityEngine.Object.Destroy(popup);
                UnityEngine.Object.Destroy(managerObject);
            }
        }

        [UnityTest]
        public IEnumerator Destroying_open_canvas_unregisters_its_scope()
        {
            var registry = new UiScopeRegistry();
            registry.Enter(UiScopePath.Of("Home"));
            var managerObject = new GameObject("UI Manager Scope Probe", typeof(UIManager));
            var canvasObject = new GameObject("Scoped canvas", typeof(PlayModeScopedCanvas));
            try
            {
                var canvas = canvasObject.GetComponent<PlayModeScopedCanvas>();
                canvas.Registry = registry;
                canvas.Open();
                UnityEngine.Object.Destroy(canvasObject);
                yield return null;

                var bound = typeof(UiScopeRegistry).GetField("_bound",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.AreEqual(0, ((System.Collections.IDictionary)bound.GetValue(registry)).Count);
            }
            finally
            {
                Locator.UI = null;
                if (canvasObject != null) UnityEngine.Object.Destroy(canvasObject);
                UnityEngine.Object.Destroy(managerObject);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.PlayModeTests
{
    public sealed class BulkOpenOnCloseCanvas : UICanvas
    {
        public static Action Closed;
        protected override void OnClose() => Closed?.Invoke();
    }

    public sealed class BulkSecondCanvas : UICanvas { }

    public sealed class UIManagerBulkOpsPlayModeTests
    {
        private sealed class MapProvider : IUIAsyncPrefabProvider
        {
            public readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
            public Task<UIPrefabLease> LoadAsync(string address) =>
                Task.FromResult(new UIPrefabLease(Prefabs[address], () => { }));
        }

        private readonly List<GameObject> created = new List<GameObject>();
        private UIManager manager;
        private MapProvider provider;

        [SetUp]
        public void SetUp()
        {
            provider = new MapProvider();
            provider.Prefabs["ui/first"] = Track(new GameObject("First", typeof(RectTransform), typeof(BulkOpenOnCloseCanvas)));
            provider.Prefabs["ui/second"] = Track(new GameObject("Second", typeof(RectTransform), typeof(BulkSecondCanvas)));
            manager = Track(new GameObject("UI Manager Bulk", typeof(UIManager))).GetComponent<UIManager>();
            manager.SetAsyncProvider(provider);
        }

        [TearDown]
        public void TearDown()
        {
            BulkOpenOnCloseCanvas.Closed = null;
            Hung.Base.ServiceSlots.Unregister<Hung.Base.IUIService>(Hung.Base.ServiceSlots.Get<Hung.Base.IUIService>());
            foreach (GameObject go in created) if (go != null) UnityEngine.Object.Destroy(go);
        }

        private GameObject Track(GameObject go) { created.Add(go); return go; }

        private T Acquire<T>(string address) where T : UICanvas
        {
            Task<UIAcquireResult<T>> pending = manager.AcquireAsync<T>(address);
            Assert.IsTrue(pending.IsCompleted, "provider completes synchronously");
            Assert.IsTrue(pending.Result.Succeeded, pending.Result.Diagnostic);
            Track(pending.Result.Canvas.gameObject);
            return pending.Result.Canvas;
        }

        // BUG-261114-f5f6: OnClose that loads another canvas must not break CloseAll enumeration.
        [Test]
        public void CloseAll_survives_OnClose_that_registers_a_new_canvas()
        {
            BulkOpenOnCloseCanvas first = Acquire<BulkOpenOnCloseCanvas>("ui/first");
            BulkOpenOnCloseCanvas.Closed = () => Acquire<BulkSecondCanvas>("ui/second");
            first.Open();

            Assert.DoesNotThrow(() => manager.CloseAll());
            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.IsTrue(manager.IsLoaded<BulkSecondCanvas>());
        }

        // BUG-261114-0da5: a destroyed isDestroyOnClose canvas must not abort DestroyAllUI.
        [UnityTest]
        public IEnumerator DestroyAllUI_skips_canvas_destroyed_on_close_and_destroys_the_rest()
        {
            BulkOpenOnCloseCanvas first = Acquire<BulkOpenOnCloseCanvas>("ui/first");
            BulkSecondCanvas second = Acquire<BulkSecondCanvas>("ui/second");
            first.isDestroyOnClose = true;
            first.Open();
            first.Close();
            yield return null;
            Assert.IsTrue(first == null, "destroyed on close");

            Assert.DoesNotThrow(() => manager.DestroyAllUI(new HashSet<UICanvas>()));
            yield return null;
            Assert.IsTrue(second == null, "remaining canvas destroyed");
            Assert.DoesNotThrow(() => manager.DestroyAllUI(null), "null exception set means none");
        }
    }
}

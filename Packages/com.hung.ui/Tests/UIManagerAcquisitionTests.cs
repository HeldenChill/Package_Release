using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.Tests
{
    public sealed class AsyncProbeCanvas : UICanvas { }

    public sealed class UIManagerAcquisitionTests
    {
        private sealed class NullResourcesProvider : IUIPrefabProvider
        {
            public T GetPrefab<T>() where T : UICanvas => null;
        }

        private sealed class DeferredProvider : IUIAsyncPrefabProvider
        {
            public int Loads;
            public int Releases;
            public readonly TaskCompletionSource<UIPrefabLease> Completion =
                new TaskCompletionSource<UIPrefabLease>();

            public Task<UIPrefabLease> LoadAsync(string address)
            {
                Loads++;
                return Completion.Task;
            }

            public UIPrefabLease Lease(GameObject prefab) =>
                new UIPrefabLease(prefab, () => Releases++);
        }

        private sealed class ImmediateProvider : IUIAsyncPrefabProvider
        {
            public GameObject Prefab;
            public int Releases;
            public Task<UIPrefabLease> LoadAsync(string address) =>
                Task.FromResult(new UIPrefabLease(Prefab, () => Releases++));
        }

        private sealed class ProbeAddressResolver : IUIAddressResolver
        {
            public string GetAddress(Type canvasType) => "ui/shop";
        }

        private GameObject prefab;
        private GameObject parentObject;
        private CanvasRegistry registry;
        private DeferredProvider provider;
        private AsyncCanvasRegistry acquisition;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("AsyncCanvasPrefab", typeof(RectTransform), typeof(AsyncProbeCanvas));
            parentObject = new GameObject("CanvasParent", typeof(RectTransform));
            registry = new CanvasRegistry(new NullResourcesProvider(), parentObject.GetComponent<RectTransform>());
            provider = new DeferredProvider();
            acquisition = new AsyncCanvasRegistry(registry, provider);
        }

        [TearDown]
        public void TearDown()
        {
            acquisition.Dispose();
            foreach (UICanvas instance in registry.All)
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance.gameObject);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(parentObject);
        }

        [Test]
        public async Task Concurrent_same_type_requests_load_once_and_share_instance()
        {
            Task<UIAcquireResult<AsyncProbeCanvas>> first = acquisition.AcquireAsync<AsyncProbeCanvas>("ui/shop");
            Task<UIAcquireResult<AsyncProbeCanvas>> second = acquisition.AcquireAsync<AsyncProbeCanvas>("ui/shop");
            Assert.AreEqual(1, provider.Loads);

            provider.Completion.SetResult(provider.Lease(prefab));
            UIAcquireResult<AsyncProbeCanvas> a = await first;
            UIAcquireResult<AsyncProbeCanvas> b = await second;

            Assert.IsTrue(a.Succeeded);
            Assert.AreSame(a.Canvas, b.Canvas);
            Assert.AreEqual(0, provider.Releases);
        }

        [Test]
        public async Task Reacquiring_destroyed_canvas_releases_previous_lease()
        {
            var immediate = new ImmediateProvider { Prefab = prefab };
            using (var cache = new AsyncCanvasRegistry(registry, immediate))
            {
                var first = await cache.AcquireAsync<AsyncProbeCanvas>("ui/shop");
                Assert.IsTrue(first.Succeeded);
                UnityEngine.Object.DestroyImmediate(first.Canvas.gameObject);

                var second = await cache.AcquireAsync<AsyncProbeCanvas>("ui/shop");
                Assert.IsTrue(second.Succeeded);
                Assert.AreEqual(1, immediate.Releases);
            }
            Assert.AreEqual(2, immediate.Releases);
        }

        [Test]
        public async Task Different_address_while_loading_fails_without_second_load()
        {
            Task<UIAcquireResult<AsyncProbeCanvas>> pending = acquisition.AcquireAsync<AsyncProbeCanvas>("ui/shop");
            UIAcquireResult<AsyncProbeCanvas> mismatch =
                await acquisition.AcquireAsync<AsyncProbeCanvas>("ui/other");

            Assert.AreEqual(UIAcquireFailure.AddressMismatch, mismatch.Failure);
            Assert.AreEqual(1, provider.Loads);
            provider.Completion.SetResult(provider.Lease(prefab));
            Assert.IsTrue((await pending).Succeeded);
        }

        [Test]
        public async Task Missing_canvas_component_releases_lease()
        {
            GameObject wrongPrefab = new GameObject("WrongPrefab");
            try
            {
                Task<UIAcquireResult<AsyncProbeCanvas>> pending = acquisition.AcquireAsync<AsyncProbeCanvas>("ui/shop");
                provider.Completion.SetResult(provider.Lease(wrongPrefab));
                UIAcquireResult<AsyncProbeCanvas> result = await pending;
                Assert.AreEqual(UIAcquireFailure.MissingCanvasComponent, result.Failure);
                Assert.AreEqual(1, provider.Releases);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrongPrefab); }
        }

        [Test]
        public async Task Dispose_during_load_releases_late_lease()
        {
            Task<UIAcquireResult<AsyncProbeCanvas>> pending = acquisition.AcquireAsync<AsyncProbeCanvas>("ui/shop");
            acquisition.Dispose();
            provider.Completion.SetResult(provider.Lease(prefab));

            UIAcquireResult<AsyncProbeCanvas> result = await pending;
            Assert.AreEqual(UIAcquireFailure.ManagerDestroyed, result.Failure);
            Assert.AreEqual(1, provider.Releases);
        }

        [Test]
        public async Task Synchronous_registration_during_load_rejects_addressed_lease()
        {
            Task<UIAcquireResult<AsyncProbeCanvas>> pending = acquisition.AcquireAsync<AsyncProbeCanvas>("ui/shop");
            AsyncProbeCanvas synchronous = registry.RegisterPrefab(prefab.GetComponent<AsyncProbeCanvas>());
            provider.Completion.SetResult(provider.Lease(prefab));

            UIAcquireResult<AsyncProbeCanvas> result = await pending;
            Assert.AreEqual(UIAcquireFailure.AddressMismatch, result.Failure);
            Assert.AreEqual(1, provider.Releases);
            Assert.AreSame(synchronous, registry.GetLoaded<AsyncProbeCanvas>());
        }

        [Test]
        public async Task Manager_exposes_addressed_acquisition_and_on_destroy_handler_releases()
        {
            var managerObject = new GameObject("UIManager");
            AsyncProbeCanvas loaded = null;
            try
            {
                UIManager manager = managerObject.AddComponent<UIManager>();
                manager.SetAsyncProvider(provider);
                Task<UIAcquireResult<AsyncProbeCanvas>> pending =
                    manager.AcquireAsync<AsyncProbeCanvas>("ui/shop");
                provider.Completion.SetResult(provider.Lease(prefab));

                UIAcquireResult<AsyncProbeCanvas> result = await pending;
                Assert.IsTrue(result.Succeeded);
                loaded = result.Canvas;
                Assert.AreSame(result.Canvas, manager.GetUI<AsyncProbeCanvas>());
                int callbacks = 0;
                manager.GetUIAsync<AsyncProbeCanvas>(canvas =>
                {
                    callbacks++;
                    Assert.AreSame(loaded, canvas);
                });
                Assert.AreEqual(1, callbacks, "Existing callback API must complete once.");
                typeof(UIManager).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(manager, null);
                Assert.AreEqual(1, provider.Releases, "OnDestroy handler must release retained prefab handle.");
            }
            finally
            {
                Locator.UI = null;
                if (loaded != null) UnityEngine.Object.DestroyImmediate(loaded.gameObject);
                if (managerObject != null) UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public async Task Configured_callback_path_waits_for_addressed_acquisition_once()
        {
            var managerObject = new GameObject("UIManager");
            AsyncProbeCanvas loaded = null;
            try
            {
                UIManager manager = managerObject.AddComponent<UIManager>();
                manager.SetAsyncProvider(provider, new ProbeAddressResolver());
                int callbacks = 0;
                var completion = new TaskCompletionSource<AsyncProbeCanvas>();
                manager.GetUIAsync<AsyncProbeCanvas>(canvas =>
                {
                    callbacks++;
                    completion.TrySetResult(canvas);
                });
                Assert.AreEqual(0, callbacks);
                Assert.AreEqual(1, provider.Loads);

                provider.Completion.SetResult(provider.Lease(prefab));
                loaded = await completion.Task;
                Assert.IsNotNull(loaded);
                Assert.AreEqual(1, callbacks);
                Assert.AreSame(loaded, manager.GetUI<AsyncProbeCanvas>());
            }
            finally
            {
                Locator.UI = null;
                if (loaded != null) UnityEngine.Object.DestroyImmediate(loaded.gameObject);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public async Task Configured_callback_path_reports_failed_prefab_once_and_releases_lease()
        {
            var managerObject = new GameObject("UIManager");
            var wrongPrefab = new GameObject("WrongPrefab");
            try
            {
                UIManager manager = managerObject.AddComponent<UIManager>();
                manager.SetAsyncProvider(provider, new ProbeAddressResolver());
                int callbacks = 0;
                var completion = new TaskCompletionSource<AsyncProbeCanvas>();
                manager.GetUIAsync<AsyncProbeCanvas>(canvas =>
                {
                    callbacks++;
                    completion.TrySetResult(canvas);
                });
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "\\[UIManager\\] UI load failed"));
                provider.Completion.SetResult(provider.Lease(wrongPrefab));

                Assert.IsNull(await completion.Task);
                Assert.AreEqual(1, callbacks);
                Assert.AreEqual(1, provider.Releases);
            }
            finally
            {
                Locator.UI = null;
                UnityEngine.Object.DestroyImmediate(managerObject);
                UnityEngine.Object.DestroyImmediate(wrongPrefab);
            }
        }

    }
}

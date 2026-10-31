using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Hung.UI.Tests
{
    public sealed class ConformanceCanvas : UICanvas { }

    public sealed class UIAcquisitionConformanceTests
    {
        private sealed class SyncProvider : IUIPrefabProvider
        {
            public ConformanceCanvas Prefab;
            public int Calls;
            public T GetPrefab<T>() where T : UICanvas { Calls++; return Prefab as T; }
        }
        private sealed class ScriptedProvider : IUIAsyncPrefabProvider
        {
            public readonly Queue<Task<UIPrefabLease>> Steps = new Queue<Task<UIPrefabLease>>();
            public int Loads, Releases;
            public Task<UIPrefabLease> LoadAsync(string address) { Loads++; return Steps.Dequeue(); }
            public UIPrefabLease Lease(GameObject prefab) => new UIPrefabLease(prefab, () => Releases++);
        }
        private GameObject prefab, parent;
        private CanvasRegistry registry;
        private SyncProvider sync;
        private ScriptedProvider provider;
        private AsyncCanvasRegistry cache;

        [SetUp]
        public void SetUp()
        {
            parent = new GameObject("UI-C0 owned parent", typeof(RectTransform));
            prefab = new GameObject("UI-C0 owned prefab", typeof(RectTransform), typeof(ConformanceCanvas));
            prefab.SetActive(false);
            sync = new SyncProvider();
            provider = new ScriptedProvider();
            registry = new CanvasRegistry(sync, parent.GetComponent<RectTransform>());
            cache = new AsyncCanvasRegistry(registry, provider);
        }

        [TearDown]
        public void TearDown()
        {
            cache.Dispose();
            UnityEngine.Object.DestroyImmediate(parent);
            UnityEngine.Object.DestroyImmediate(prefab);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task Invalid_address_never_invokes_provider(string address)
        {
            var result = await cache.AcquireAsync<ConformanceCanvas>(address);
            Assert.AreEqual(UIAcquireFailure.InvalidAddress, result.Failure);
            Assert.AreEqual(0, provider.Loads);
        }

        [Test]
        public async Task Failed_provider_can_retry_without_cached_failure()
        {
            provider.Steps.Enqueue(Task.FromException<UIPrefabLease>(new InvalidOperationException("UI-C0 injected load failure")));
            provider.Steps.Enqueue(Task.FromResult(provider.Lease(prefab)));
            var failed = await cache.AcquireAsync<ConformanceCanvas>("ui/c0");
            Assert.AreEqual(UIAcquireFailure.LoadFailed, failed.Failure);
            StringAssert.Contains("UI-C0 injected load failure", failed.Diagnostic);
            var success = await cache.AcquireAsync<ConformanceCanvas>("ui/c0");
            Assert.IsTrue(success.Succeeded, success.Diagnostic);
            Assert.AreEqual(2, provider.Loads);
            Assert.AreEqual(0, provider.Releases);
        }

        [Test]
        public async Task Loaded_type_rejects_other_address_without_loading_again()
        {
            provider.Steps.Enqueue(Task.FromResult(provider.Lease(prefab)));
            var first = await cache.AcquireAsync<ConformanceCanvas>("ui/c0");
            Assert.IsTrue(first.Succeeded);
            var mismatch = await cache.AcquireAsync<ConformanceCanvas>("ui/other");
            Assert.AreEqual(UIAcquireFailure.AddressMismatch, mismatch.Failure);
            Assert.AreSame(first.Canvas, registry.GetLoaded<ConformanceCanvas>());
            Assert.AreEqual(1, provider.Loads);
        }

        [Test]
        public async Task Acquire_does_not_open_and_dispose_releases_once()
        {
            provider.Steps.Enqueue(Task.FromResult(provider.Lease(prefab)));
            var result = await cache.AcquireAsync<ConformanceCanvas>("ui/c0");
            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.Canvas.gameObject.activeSelf);
            Assert.AreEqual(0, provider.Releases);
            cache.Dispose();
            cache.Dispose();
            Assert.AreEqual(1, provider.Releases);
        }

        [Test]
        public async Task Disposed_cache_rejects_without_provider_call()
        {
            cache.Dispose();
            var result = await cache.AcquireAsync<ConformanceCanvas>("ui/c0");
            Assert.AreEqual(UIAcquireFailure.ManagerDestroyed, result.Failure);
            Assert.AreEqual(0, provider.Loads);
        }

        [Test]
        public void Sync_registry_caches_instance_and_preserves_parent()
        {
            sync.Prefab = prefab.GetComponent<ConformanceCanvas>();
            var first = registry.Get<ConformanceCanvas>();
            Assert.AreSame(first, registry.Get<ConformanceCanvas>());
            Assert.AreSame(parent.transform, first.transform.parent);
            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreEqual(1, sync.Calls);
        }

        [Test]
        public void Sync_registry_missing_prefab_has_explicit_diagnostic()
        {
            var error = Assert.Throws<InvalidOperationException>(() => registry.Get<ConformanceCanvas>());
            StringAssert.Contains(nameof(ConformanceCanvas), error.Message);
        }

        [Test]
        public void Lease_release_is_idempotent()
        {
            var lease = provider.Lease(prefab);
            lease.Dispose();
            lease.Dispose();
            Assert.AreEqual(1, provider.Releases);
        }
    }
}

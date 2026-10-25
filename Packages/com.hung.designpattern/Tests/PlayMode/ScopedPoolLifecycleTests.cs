using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hung.DesignPattern.ScopedPooling.Tests
{
    public sealed class LifecycleProbe : MonoBehaviour { }

    public sealed class ReentryLifecycleProbe : MonoBehaviour
    {
        public static System.Action Enabled;
        private void OnEnable() => Enabled?.Invoke();
    }

    public class ScopedPoolLifecycleTests
    {
        [UnityTest]
        public IEnumerator Spawn_OnEnableDisposal_ReturnsDisposedScope()
        {
            var host = new GameObject("host");
            var source = new GameObject("source");
            var prefab = source.AddComponent<ReentryLifecycleProbe>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("units", new PoolDefinition(prefab, PoolKind.Unit));
                ReentryLifecycleProbe.Enabled = scope.Dispose;

                var status = scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var checkout);

                Assert.AreEqual(PoolStatus.DisposedScope, status);
                Assert.IsNull(checkout.Instance);
                Assert.IsTrue(host != null);
                yield return null;
            }
            finally
            {
                ReentryLifecycleProbe.Enabled = null;
                scope.Dispose();
                Object.Destroy(source);
                Object.Destroy(host);
            }
        }

        [UnityTest]
        public IEnumerator SceneUnloadDisposes_ApplicationScopeSurvives()
        {
            var scene = SceneManager.CreateScene("P1-scene");
            var sceneHost = new GameObject("scene-host");
            SceneManager.MoveGameObjectToScene(sceneHost, scene);
            var appHost = new GameObject("app-host");
            Assert.AreEqual(PoolStatus.InvalidRoot, PoolScope.TryCreate(appHost.transform, PoolLifetime.Application, out _));
            Object.DontDestroyOnLoad(appHost);
            PoolScope.TryCreate(sceneHost.transform, PoolLifetime.Scene, out var sceneScope);
            PoolScope.TryCreate(appHost.transform, PoolLifetime.Application, out var appScope);
            var source = new GameObject("source");
            var prefab = source.AddComponent<LifecycleProbe>();
            try
            {
                sceneScope.Register("scene", new PoolDefinition(prefab, PoolKind.Unit, warmup: 2));
                appScope.Register("app", new PoolDefinition(prefab, PoolKind.Unit, warmup: 2));
                var sceneInstances = sceneHost.GetComponentsInChildren<LifecycleProbe>(true);
                var appInstances = appHost.GetComponentsInChildren<LifecycleProbe>(true);
                Assert.AreEqual(2, sceneInstances.Length);
                Assert.AreEqual(2, appInstances.Length);
                sceneScope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var sceneToken);
                appScope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var appToken);
                sceneScope.ScheduleReturn(sceneToken, 100, PoolClock.Unscaled, out var sceneRequest);
                appScope.ScheduleReturn(appToken, 100, PoolClock.Unscaled, out var appRequest);
                yield return SceneManager.UnloadSceneAsync(scene);
                foreach (var instance in sceneInstances) Assert.IsTrue(instance == null);
                Assert.IsFalse(sceneRequest.IsValid);
                Assert.IsFalse(sceneToken.IsValid);
                Assert.AreEqual(PoolStatus.DisposedScope, sceneScope.Return(sceneToken));
                Assert.IsTrue(appRequest.IsValid);
                Assert.IsTrue(appToken.IsValid);
                appScope.Dispose();
                yield return null;
                Assert.IsTrue(appHost != null);
                Assert.IsTrue(appToken.Instance == null);
                foreach (var instance in appInstances) Assert.IsTrue(instance == null);
            }
            finally
            {
                sceneScope.Dispose();
                appScope.Dispose();
                Object.Destroy(source);
                Object.Destroy(appHost);
            }
        }

        [UnityTest]
        public IEnumerator HostRootDestruction_InvalidatesCheckoutAndPendingRequest()
        {
            var host = new GameObject("host");
            var source = new GameObject("source");
            var prefab = source.AddComponent<LifecycleProbe>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("units", new PoolDefinition(prefab, PoolKind.Unit));
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var token);
                scope.ScheduleReturn(token, 100, PoolClock.Unscaled, out var request);
                Object.Destroy(host);
                yield return null;
                Assert.IsFalse(token.IsValid);
                Assert.IsFalse(request.IsValid);
                Assert.AreEqual(PoolStatus.DisposedScope,
                    scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out _));
            }
            finally
            {
                scope.Dispose();
                Object.Destroy(source);
                if (host != null) Object.Destroy(host);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeUnscaledTimerReturns_WhileScaledClockPaused()
        {
            var host = new GameObject("host");
            var source = new GameObject("source");
            var prefab = source.AddComponent<LifecycleProbe>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            float previousScale = Time.timeScale;
            try
            {
                scope.Register("units", new PoolDefinition(prefab, PoolKind.Unit));
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var token);
                Time.timeScale = 0;
                scope.ScheduleReturn(token, .3f, PoolClock.Unscaled, out _);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.IsFalse(token.IsValid);
                scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out var next);
                scope.ScheduleReturn(next, .01f, PoolClock.Scaled, out _);
                yield return new WaitForSecondsRealtime(.05f);
                Assert.IsTrue(next.IsValid);
            }
            finally
            {
                Time.timeScale = previousScale;
                scope.Dispose();
                Object.Destroy(source);
                Object.Destroy(host);
            }
        }
    }
}

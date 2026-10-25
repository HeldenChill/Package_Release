using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Hung.DesignPattern.ScopedPooling;

namespace Hung.DesignPattern.Tests
{
    public class ScopedPoolAllocationFailureTests
    {
        [Test]
        public void SecondWarmupFailure_CleansAttemptOnly()
        {
            var host = new GameObject("host");
            var source = new GameObject("source");
            var earlierSource = new GameObject("earlier-source");
            var prefab = source.AddComponent<ScopedPoolProbe>();
            var earlierPrefab = earlierSource.AddComponent<ScopedPoolProbe>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                scope.Register("earlier", new PoolDefinition(earlierPrefab, PoolKind.Unit, "earlier", 1));
                scope.Spawn(earlierPrefab, Vector3.zero, Quaternion.identity, null, out var earlierCheckout);
                int calls = 0;
                Func<Component, Transform, Component> allocate = (p, parent) =>
                {
                    if (++calls == 2) throw new InvalidOperationException("injected-allocation-failure");
                    return UnityEngine.Object.Instantiate(p, parent);
                };
                var field = typeof(PoolScope).GetField("instantiate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(field);
                field.SetValue(scope, allocate);
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("injected-allocation-failure"));
                Assert.AreEqual(PoolStatus.AllocationFailed,
                    scope.Register("retry", new PoolDefinition(prefab, PoolKind.Unit, "unit", 2)));
                Assert.AreEqual(1, host.transform.childCount, "Only the earlier group's owned subtree may remain");
                Assert.IsTrue(earlierCheckout.IsValid);
                StringAssert.Contains("group=retry", scope.LastDiagnostic);
                Assert.AreEqual(PoolStatus.Success,
                    scope.Register("retry", new PoolDefinition(prefab, PoolKind.Unit, "unit", 1)));
                Assert.IsTrue(host != null);
                Assert.AreEqual(PoolStatus.Success,
                    scope.Spawn(prefab, Vector3.zero, Quaternion.identity, null, out _));
            }
            finally
            {
                scope.Dispose();
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(earlierSource);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}

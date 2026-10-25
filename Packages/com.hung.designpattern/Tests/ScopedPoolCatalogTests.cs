using NUnit.Framework;
using UnityEngine;
using Hung.DesignPattern.ScopedPooling;

namespace Hung.DesignPattern.Tests
{
    public class ScopedPoolCatalogTests
    {
        [Test]
        public void Snapshot_IsFrozen_AndMissingOrWrongTypeIsExplicit()
        {
            var host = new GameObject("host");
            var source = new GameObject("source");
            var prefab = source.AddComponent<ScopedPoolProbe>();
            var catalog = ScriptableObject.CreateInstance<PoolCatalogAsset>();
            PoolScope.TryCreate(host.transform, PoolLifetime.Scene, out var scope);
            try
            {
                catalog.Configure(new PoolCatalogEntry { Identity = "product.unit", Prefab = prefab, Kind = PoolKind.Unit });
                Assert.AreEqual(PoolStatus.Success, scope.Register("catalog", catalog));
                catalog.Configure(new PoolCatalogEntry { Identity = "renamed", Prefab = prefab, Kind = PoolKind.Unit });
                Assert.AreEqual(PoolStatus.Success, scope.Spawn<ScopedPoolProbe>(new UnitPoolId("product.unit"), Vector3.zero, Quaternion.identity, null, out _));
                Assert.AreEqual(PoolStatus.UnknownIdentity, scope.Spawn<ScopedPoolProbe>(new UnitPoolId("renamed"), Vector3.zero, Quaternion.identity, null, out _));
                Assert.AreEqual(PoolStatus.WrongPrefabType, scope.Spawn<ParticleSystem>(new UnitPoolId("product.unit"), Vector3.zero, Quaternion.identity, null, out _));
            }
            finally
            {
                scope.Dispose();
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(host);
            }
        }
    }
}

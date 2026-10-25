using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Hung.DesignPattern.ScopedPooling;

namespace Hung.DesignPattern.Tests
{
    [ExecuteAlways]
    public sealed class ScopedPoolProbe : MonoBehaviour
    {
        public Vector3 EnabledPosition;
        public Transform EnabledParent;
        private void OnEnable() { EnabledPosition = transform.position; EnabledParent = transform.parent; }
    }

    [ExecuteAlways]
    public sealed class ScopedPoolReentryProbe : MonoBehaviour
    {
        public static System.Action Enabled;
        private void OnEnable() => Enabled?.Invoke();
    }

    public class ScopedPoolTests
    {
        private GameObject root, prefab;
        private PoolScope scope;

        [SetUp]
        public void SetUp()
        {
            ScopedPoolReentryProbe.Enabled = null;
            root = new GameObject("host");
            prefab = new GameObject("source");
            prefab.AddComponent<ScopedPoolProbe>();
            Assert.AreEqual(PoolStatus.Success, PoolScope.TryCreate(root.transform, PoolLifetime.Scene, out scope));
        }

        [TearDown]
        public void TearDown()
        {
            ScopedPoolReentryProbe.Enabled = null;
            scope?.Dispose();
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PoseBeforeActivation_AndDuplicateReturnAreSafe()
        {
            var source = prefab.GetComponent<ScopedPoolProbe>();
            Assert.AreEqual(PoolStatus.Success, scope.Register("units", new PoolDefinition(source, PoolKind.Unit)));
            var position = new Vector3(7, 8, 9);
            scope.Spawn(source, position, Quaternion.identity, null, out var first);
            Assert.AreEqual(position, ((ScopedPoolProbe)first.Instance).EnabledPosition);
            Assert.AreEqual(PoolStatus.Success, scope.Return(first));
            Assert.AreEqual(PoolStatus.AlreadyReturned, scope.Return(first));
            scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var second);
            Assert.AreSame(first.Instance, second.Instance);
            Assert.AreEqual(PoolStatus.StaleCheckout, scope.Return(first));
            Assert.IsTrue(second.IsValid);
        }

        [Test]
        public void RegistrationFailure_PreservesEarlierGroupAndRoot()
        {
            var source = prefab.GetComponent<ScopedPoolProbe>();
            scope.Register("ok", new PoolDefinition(source, PoolKind.Unit, "one", 1, 1));
            Assert.AreEqual(PoolStatus.DuplicateGroup, scope.Register("ok", new PoolDefinition(source, PoolKind.Unit)));
            Assert.AreEqual(PoolStatus.InvalidConfiguration, scope.Register("bad", new PoolDefinition(null, PoolKind.Unit)));
            Assert.AreEqual(PoolStatus.Success, scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var token));
            Assert.AreEqual(PoolStatus.CapacityExhausted, scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out _));
            scope.Dispose();
            Assert.IsTrue(root != null);
            Assert.IsFalse(token.IsValid);
            Assert.AreEqual(PoolStatus.DisposedScope, scope.Return(token));
        }

        [Test]
        public void SamePrefabInTwoScopes_HasIndependentOwnership()
        {
            var otherRoot = new GameObject("other-host");
            PoolScope.TryCreate(otherRoot.transform, PoolLifetime.Scene, out var other);
            try
            {
                var source = prefab.GetComponent<ScopedPoolProbe>();
                scope.Register("a", new PoolDefinition(source, PoolKind.Unit));
                other.Register("b", new PoolDefinition(source, PoolKind.Unit));
                scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var first);
                other.Spawn(source, Vector3.zero, Quaternion.identity, null, out var second);
                Assert.AreEqual(PoolStatus.WrongScope, other.Return(first));
                scope.Dispose();
                Assert.IsTrue(second.IsValid);
            }
            finally
            {
                other.Dispose();
                Object.DestroyImmediate(otherRoot);
            }
        }

        [Test]
        public void WarmupIsNotCapacity_AndWrongTypeDoesNotActivate()
        {
            var source = prefab.GetComponent<ScopedPoolProbe>();
            scope.Register("grow", new PoolDefinition(source, PoolKind.Unit, "unit", 1));
            Assert.AreEqual(PoolStatus.WrongPrefabType,
                scope.Spawn<ParticleSystem>(new UnitPoolId("unit"), Vector3.zero, Quaternion.identity, null, out _));
            scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var a);
            scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var b);
            Assert.AreNotSame(a.Instance, b.Instance);
            Assert.AreEqual(PoolStatus.Success, scope.Collect("grow"));
            Assert.IsFalse(a.IsValid);
            Assert.IsFalse(b.IsValid);
        }

        [Test]
        public void InvalidWholeGroupPublishesNothing()
        {
            var source = prefab.GetComponent<ScopedPoolProbe>();
            Assert.AreEqual(PoolStatus.DuplicateIdentity, scope.Register("bad",
                new PoolDefinition(source, PoolKind.Unit, "duplicate", 1),
                new PoolDefinition(root.transform, PoolKind.Unit, "duplicate", 1)));
            Assert.AreEqual(PoolStatus.UnknownIdentity,
                scope.Spawn<ScopedPoolProbe>(new UnitPoolId("duplicate"), Vector3.zero, Quaternion.identity, null, out _));
            Assert.AreEqual(PoolStatus.Success, scope.Register("bad", new PoolDefinition(source, PoolKind.Unit)));
        }

        [Test]
        public void Spawn_OnEnableCollect_RejectsReturnedCheckout()
        {
            var source = prefab.AddComponent<ScopedPoolReentryProbe>();
            scope.Register("units", new PoolDefinition(source, PoolKind.Unit));
            ScopedPoolReentryProbe.Enabled = () => scope.Collect("units");

            var status = scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var checkout);

            Assert.AreEqual(PoolStatus.StaleCheckout, status);
            Assert.IsNull(checkout.Instance);
        }

        [Test]
        public void Spawn_OnEnableRecheckout_PreservesNestedGeneration()
        {
            var source = prefab.AddComponent<ScopedPoolReentryProbe>();
            scope.Register("units", new PoolDefinition(source, PoolKind.Unit, hardLimit: 1));
            PoolCheckout nested = default;
            var nestedPosition = new Vector3(9, 8, 7);
            ScopedPoolReentryProbe.Enabled = () =>
            {
                ScopedPoolReentryProbe.Enabled = null;
                scope.Collect("units");
                Assert.AreEqual(PoolStatus.Success,
                    scope.Spawn(source, nestedPosition, Quaternion.identity, null, out nested));
            };

            var status = scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var checkout);

            Assert.AreEqual(PoolStatus.StaleCheckout, status);
            Assert.IsNull(checkout.Instance);
            Assert.IsTrue(nested.IsValid);
            Assert.AreEqual(nestedPosition, nested.Instance.transform.position);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestroyedInstances_SpawnRetiresSlotsAndRequests(bool returnBeforeDestroy)
        {
            var source = prefab.GetComponent<ScopedPoolProbe>();
            scope.Register("units", new PoolDefinition(source, PoolKind.Unit, hardLimit: 1));
            scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var previous);
            for (int i = 0; i < 5; i++)
            {
                scope.ScheduleReturn(previous, 100, PoolClock.Unscaled, out var request);
                if (returnBeforeDestroy) scope.Return(previous);
                Object.DestroyImmediate(previous.Instance.gameObject);

                Assert.AreEqual(PoolStatus.Success,
                    scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var replacement));

                Assert.AreEqual(1, SlotCount());
                Assert.IsFalse(request.IsValid);
                Assert.AreEqual(PoolStatus.StaleCheckout, scope.Return(previous));
                Assert.IsTrue(replacement.IsValid);
                previous = replacement;
            }
        }

        [Test]
        public void DestroyedActiveCheckout_TickRetiresSlotBeforeDelayExpires()
        {
            var source = prefab.GetComponent<ScopedPoolProbe>();
            scope.Register("units", new PoolDefinition(source, PoolKind.Unit));
            scope.Spawn(source, Vector3.zero, Quaternion.identity, null, out var checkout);
            scope.ScheduleReturn(checkout, 100, PoolClock.Unscaled, out var request);
            Object.DestroyImmediate(checkout.Instance.gameObject);

            scope.Tick(0, 0);

            Assert.AreEqual(0, SlotCount());
            Assert.IsFalse(request.IsValid);
        }

        private int SlotCount() => ((IDictionary)typeof(PoolScope)
            .GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scope)).Count;
    }
}

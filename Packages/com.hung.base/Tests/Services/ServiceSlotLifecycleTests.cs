using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Base.Tests
{
    public sealed class ServiceOwnerProbe : MonoBehaviour { }
    public sealed class ServiceSlotLifecycleTests
    {
        private GameObject root;
        private ServiceOwnerProbe owner;
        private sealed class DisposableProbe : IDisposable { public void Dispose() { } }
        [SetUp] public void SetUp()
        {
            ServiceSlots.ResetForTests(); root=new GameObject("slot-lifecycle-test"); owner=root.AddComponent<ServiceOwnerProbe>();
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(root); ServiceSlots.ResetForTests(); }
        [Test] public void EmptySlot_CanPrepareWithoutPublishing()
        {
            var service=new DisposableProbe(); Assert.IsTrue(ServiceSlotLifecycle.CanInitialize<IDisposable>(service,owner));
            Assert.IsFalse(ServiceSlots.TryGet<IDisposable>(out _)); Assert.IsTrue(owner.enabled);
        }
        [Test] public void SameOwnerDuplicate_IsDisabledBeforeAnyInitialization()
        {
            ServiceSlots.Register<IDisposable>(new DisposableProbe());
            LogAssert.Expect(LogType.Error,"[ServiceSlots] Active scene owner already exists for IDisposable.");
            Assert.IsFalse(ServiceSlotLifecycle.CanInitialize<IDisposable>(new DisposableProbe(),owner));
            Assert.IsFalse(owner.enabled);
        }
        [Test] public void ForeignOwner_IsKeptAndNewComponentDisabled()
        {
            using(var old=new MemoryStream())
            {
                ServiceSlots.Register<IDisposable>(old);
                LogAssert.Expect(LogType.Error,"[ServiceSlots] IDisposable is owned by "+old.GetType().Assembly.GetName().Name+
                    "; refused registration from "+typeof(DisposableProbe).Assembly.GetName().Name+".");
                Assert.IsFalse(ServiceSlotLifecycle.CanInitialize<IDisposable>(new DisposableProbe(),owner));
                Assert.AreSame(old,ServiceSlots.Get<IDisposable>()); Assert.IsFalse(owner.enabled);
            }
        }
    }
}

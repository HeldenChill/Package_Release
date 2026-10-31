using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Base.Tests
{
    public sealed class ServiceSlotsTests
    {
        private interface IProbe { }
        private sealed class ProbeA : IProbe { }
        private sealed class ProbeB : IProbe { }
        private sealed class DisposableProbe : IDisposable
        {
            public void Dispose() { }
        }

        [SetUp]
        public void SetUp() => ServiceSlots.ResetForTests();

        [TearDown]
        public void TearDown() => ServiceSlots.ResetForTests();

        [Test]
        public void EmptySlot_HasNoValueOrOwner()
        {
            Assert.IsNull(ServiceSlots.Get<IProbe>());
            Assert.IsFalse(ServiceSlots.TryGet<IProbe>(out var service));
            Assert.IsNull(service);
            Assert.IsNull(ServiceSlots.OwnerOf<IProbe>());
        }

        [Test]
        public void Register_EmptySlot_RecordsInstanceAndImplementationOwner()
        {
            var service = new ProbeA();
            Assert.AreEqual(ServiceRegistration.Registered,
                ServiceSlots.Register<IProbe>(service));
            Assert.AreSame(service, ServiceSlots.Get<IProbe>());
            Assert.IsTrue(ServiceSlots.TryGet<IProbe>(out var found));
            Assert.AreSame(service, found);
            Assert.AreEqual(typeof(ProbeA).Assembly.GetName().Name,
                ServiceSlots.OwnerOf<IProbe>());
        }

        [Test]
        public void Register_SameInstance_IsIdempotent()
        {
            var service = new ProbeA();
            ServiceSlots.Register<IProbe>(service);
            Assert.AreEqual(ServiceRegistration.AlreadyRegistered,
                ServiceSlots.Register<IProbe>(service));
        }

        [Test]
        public void Register_SameOwnerNewInstance_Replaces()
        {
            ServiceSlots.Register<IProbe>(new ProbeA());
            var replacement = new ProbeB();
            Assert.AreEqual(ServiceRegistration.Replaced,
                ServiceSlots.Register<IProbe>(replacement));
            Assert.AreSame(replacement, ServiceSlots.Get<IProbe>());
        }

        [Test]
        public void Register_DifferentOwner_IsRefusedWithoutMutation()
        {
            using (var original = new MemoryStream())
            {
                var replacement = new DisposableProbe();
                string owner = original.GetType().Assembly.GetName().Name;
                string other = replacement.GetType().Assembly.GetName().Name;
                Assert.AreNotEqual(owner, other, "Test requires two implementation assemblies.");
                ServiceSlots.Register<IDisposable>(original);
                LogAssert.Expect(LogType.Error,
                    $"[ServiceSlots] IDisposable is owned by {owner}; refused registration from {other}.");
                Assert.AreEqual(ServiceRegistration.RefusedDuplicateOwner,
                    ServiceSlots.Register<IDisposable>(replacement));
                Assert.AreSame(original, ServiceSlots.Get<IDisposable>());
                Assert.AreEqual(owner, ServiceSlots.OwnerOf<IDisposable>());
            }
        }

        [Test]
        public void Register_Null_IsRefusedWithoutChangingCurrent()
        {
            var original = new ProbeA();
            ServiceSlots.Register<IProbe>(original);
            LogAssert.Expect(LogType.Error, "[ServiceSlots] Cannot register null for IProbe.");
            Assert.AreEqual(ServiceRegistration.RefusedNull, ServiceSlots.Register<IProbe>(null));
            Assert.AreSame(original, ServiceSlots.Get<IProbe>());
        }

        [Test]
        public void Unregister_CurrentInstance_RemovesValueAndOwner()
        {
            var service = new ProbeA();
            ServiceSlots.Register<IProbe>(service);
            Assert.IsTrue(ServiceSlots.Unregister<IProbe>(service));
            Assert.IsNull(ServiceSlots.Get<IProbe>());
            Assert.IsNull(ServiceSlots.OwnerOf<IProbe>());
        }

        [Test]
        public void Unregister_StaleInstance_KeepsReplacement()
        {
            var old = new ProbeA();
            var current = new ProbeB();
            ServiceSlots.Register<IProbe>(old);
            ServiceSlots.Register<IProbe>(current);
            Assert.IsFalse(ServiceSlots.Unregister<IProbe>(old));
            Assert.AreSame(current, ServiceSlots.Get<IProbe>());
        }

        [Test]
        public void Unregister_EmptyOrNull_ReturnsFalse()
        {
            Assert.IsFalse(ServiceSlots.Unregister<IProbe>(new ProbeA()));
            Assert.IsFalse(ServiceSlots.Unregister<IProbe>(null));
        }

        [Test]
        public void Registration_IsIsolatedByContractType()
        {
            var first = new ProbeA();
            var second = new DisposableProbe();
            ServiceSlots.Register<IProbe>(first);
            ServiceSlots.Register<IDisposable>(second);
            ServiceSlots.Unregister<IProbe>(first);
            Assert.AreSame(second, ServiceSlots.Get<IDisposable>());
        }

        [Test]
        public void ResetForTests_ClearsRegistrationsAndOwners()
        {
            ServiceSlots.Register<IProbe>(new ProbeA());
            ServiceSlots.ResetForTests();
            Assert.IsNull(ServiceSlots.Get<IProbe>());
            Assert.IsNull(ServiceSlots.OwnerOf<IProbe>());
        }

        [Test]
        public void RuntimeReset_HasSubsystemHookAndClearsState()
        {
            var method = typeof(ServiceSlots).GetMethod("ResetForPlay",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            Assert.IsNull(method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>());
            var hostReset = typeof(Hung.Base.Composition.AutomaticPluginHost).GetMethod("ResetForPlay", BindingFlags.NonPublic | BindingFlags.Static);
            var attribute = hostReset.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.IsNotNull(attribute);
            Assert.AreEqual(RuntimeInitializeLoadType.SubsystemRegistration, attribute.loadType);
            ServiceSlots.Register<IProbe>(new ProbeA());
            method.Invoke(null, null);
            Assert.IsNull(ServiceSlots.Get<IProbe>());
            Assert.IsNull(ServiceSlots.OwnerOf<IProbe>());
        }
    }
}

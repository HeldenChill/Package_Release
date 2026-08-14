using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests
{
    // Locator.Data is a private static field behind a public get/set property (Base
    // namespace, partial class spread across per-domain files) - ResetDataForTests()
    // was added to Locator.Data.cs (Packages/com.hung.data/Runtime/Base/Locator.Data.cs)
    // specifically to give tests a clean-slate hook, user-approved production change.
    public class LocatorCycleTests
    {
        private class FakeDataService : IDataService
        {
            public T GetData<T>(int index = 0) where T : class => null;
            public T GetSOData<T>() where T : ScriptableObject => null;
            public T GetUnit<T>(int type) where T : class => null;
            public void Save() { }
        }

        [TearDown]
        public void TearDown()
        {
            Hung.Base.Locator.ResetDataForTests();
        }

        [Test]
        public void Register_Resolve_ReturnsSameInstance()
        {
            var service = new FakeDataService();

            Hung.Base.Locator.Data = service;

            Assert.AreSame(service, Hung.Base.Locator.Data);
        }

        [Test]
        public void Reset_ClearsRegisteredInstance()
        {
            Hung.Base.Locator.Data = new FakeDataService();

            Hung.Base.Locator.ResetDataForTests();

            Assert.IsNull(Hung.Base.Locator.Data);
        }

        [Test]
        public void RegisterAfterReset_ResolvesNewInstance()
        {
            var first = new FakeDataService();
            Hung.Base.Locator.Data = first;
            Hung.Base.Locator.ResetDataForTests();

            var second = new FakeDataService();
            Hung.Base.Locator.Data = second;

            Assert.AreSame(second, Hung.Base.Locator.Data);
            Assert.AreNotSame(first, Hung.Base.Locator.Data);
        }
    }
}

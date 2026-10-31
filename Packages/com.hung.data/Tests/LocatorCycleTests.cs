using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests
{
    // Data reset removes only the current explicit registration.
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

            Hung.Base.ServiceSlots.Unregister<IDataService>(Hung.Base.ServiceSlots.Get<IDataService>()); Hung.Base.ServiceSlots.Register<IDataService>(service);

            Assert.AreSame(service, Hung.Base.Locator.Data);
        }

        [Test]
        public void Reset_ClearsRegisteredInstance()
        {
            Hung.Base.ServiceSlots.Unregister<IDataService>(Hung.Base.ServiceSlots.Get<IDataService>()); Hung.Base.ServiceSlots.Register<IDataService>(new FakeDataService());

            Hung.Base.Locator.ResetDataForTests();

            Assert.IsNull(Hung.Base.Locator.Data);
        }

        [Test]
        public void RegisterAfterReset_ResolvesNewInstance()
        {
            var first = new FakeDataService();
            Hung.Base.ServiceSlots.Unregister<IDataService>(Hung.Base.ServiceSlots.Get<IDataService>()); Hung.Base.ServiceSlots.Register<IDataService>(first);
            Hung.Base.Locator.ResetDataForTests();

            var second = new FakeDataService();
            Hung.Base.ServiceSlots.Unregister<IDataService>(Hung.Base.ServiceSlots.Get<IDataService>()); Hung.Base.ServiceSlots.Register<IDataService>(second);

            Assert.AreSame(second, Hung.Base.Locator.Data);
            Assert.AreNotSame(first, Hung.Base.Locator.Data);
        }
    }
}

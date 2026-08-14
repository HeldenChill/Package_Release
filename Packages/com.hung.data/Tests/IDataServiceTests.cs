using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Hung.Base;

namespace Hung.Data.Tests
{
    public class IDataServiceTests
    {
        private sealed class Marker : ScriptableObject
        {
        }

        private sealed class LegacyDataService : IDataService
        {
            public readonly Marker Marker = ScriptableObject.CreateInstance<Marker>();

            public T GetData<T>(int index = 0) where T : class => null;
            public T GetSOData<T>() where T : ScriptableObject => Marker as T;
            public T GetUnit<T>(int type) where T : class => null;
            public void Save()
            {
            }
        }

        [Test]
        public void GetSOData_ExposesOptionalIdParameter()
        {
            MethodInfo method = typeof(IDataService).GetMethod(
                "GetSOData",
                new[] { typeof(string) }).MakeGenericMethod(typeof(Marker));

            Assert.IsNotNull(method);
            ParameterInfo parameter = method.GetParameters()[0];
            Assert.AreEqual("id", parameter.Name);
            Assert.IsTrue(parameter.HasDefaultValue);
            Assert.IsNull(parameter.DefaultValue);
        }

        [Test]
        public void GetSOData_WithId_PreservesLegacyParameterlessCompatibility()
        {
            var service = new LegacyDataService();
            MethodInfo method = typeof(IDataService).GetMethod(
                "GetSOData",
                new[] { typeof(string) }).MakeGenericMethod(typeof(Marker));

            Assert.IsNotNull(method);
            var result = method.Invoke(service, new object[] { "item" });

            Assert.AreSame(service.Marker, result);
            Object.DestroyImmediate(service.Marker);
        }
    }
}

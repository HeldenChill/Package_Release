using System;
using System.ComponentModel;
using Hung.Base;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class ItemIdConverterRegressionTests
    {
        private sealed class Envelope { public ItemId Id; }

        [Test]
        public void Newtonsoft_NonStringToken_ThrowsTokenMessage()
        {
            var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Envelope>("{\"Id\":123}"));
            StringAssert.Contains("Invalid ItemId token 'Integer'.", ex.Message);
        }

        [Test]
        public void Newtonsoft_InvalidValue_ThrowsValueMessage()
        {
            var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Envelope>("{\"Id\":\"not valid\"}"));
            StringAssert.Contains("Invalid ItemId value 'not valid'.", ex.Message);
        }

        [Test]
        public void TypeConverter_RoundTripsAndRejectsInvalid()
        {
            TypeConverter c = TypeDescriptor.GetConverter(typeof(ItemId));
            Assert.That(c.ConvertFromString("base.gold"), Is.EqualTo(BaseItemIds.Gold));
            Assert.That(c.ConvertToString(BaseItemIds.Gold), Is.EqualTo("base.gold"));
            Assert.Throws<ArgumentException>(() => c.ConvertFromString("not valid"));
        }

        [Test]
        public void Newtonsoft_WritesPlainString() =>
            Assert.That(JsonConvert.SerializeObject(new Envelope { Id = BaseItemIds.Gold }), Is.EqualTo("{\"Id\":\"base.gold\"}"));
    }
}

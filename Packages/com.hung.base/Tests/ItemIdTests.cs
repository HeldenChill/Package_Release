using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public class ItemIdTests
    {
        private sealed class Envelope
        {
            public ItemId Value;
            public Dictionary<ItemId, int> Quantities = new();
        }

        [TestCase("base.gold")]
        [TestCase("pet_vs_monster.gem")]
        [TestCase("game.item-2")]
        // Namespace is optional: catalog ids built from CSV names parse unchanged.
        [TestCase("gold")]
        [TestCase("Pump_Shotgun")]
        [TestCase("Health_Kit")]
        // Case is unrestricted in both segments.
        [TestCase("Base.gold")]
        [TestCase("base.GOLD")]
        public void Parse_ValidId_RoundTrips(string raw)
        {
            ItemId id = ItemId.Parse(raw);

            Assert.AreEqual(raw, id.Value);
            Assert.IsTrue(id.IsValid);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("base..gold")]
        [TestCase("base.gold!")]
        [TestCase("base.gold.extra")]
        [TestCase("_gold")]
        [TestCase("2gold")]
        [TestCase(".gold")]
        [TestCase("gold.")]
        public void Parse_InvalidId_Throws(string raw)
        {
            Assert.Throws<ArgumentException>(() => ItemId.Parse(raw));
        }

        /// <summary>
        /// Case is allowed by the pattern but NOT folded by equality — the widened regex
        /// must not be mistaken for case-insensitive ids.
        /// </summary>
        [Test]
        public void Equality_IsCaseSensitive()
        {
            Assert.AreNotEqual(ItemId.Parse("base.gold"), ItemId.Parse("base.Gold"));
            Assert.AreNotEqual(ItemId.Parse("Pump_Shotgun"), ItemId.Parse("pump_shotgun"));
        }

        [Test]
        public void Equality_UsesOrdinalValue()
        {
            Assert.AreEqual(ItemId.Parse("base.gold"), ItemId.Parse("base.gold"));
            Assert.AreNotEqual(ItemId.Parse("base.gold"), ItemId.Parse("base.heart"));
        }

        [Test]
        public void Default_IsInvalid()
        {
            Assert.IsFalse(default(ItemId).IsValid);
        }

        [Test]
        public void Newtonsoft_RoundTripsValueAndDictionaryKey()
        {
            var source = new Envelope { Value = BaseItemIds.Gold };
            source.Quantities[ItemId.Parse("pet_vs_monster.gem")] = 7;

            string json = JsonConvert.SerializeObject(source);
            var loaded = JsonConvert.DeserializeObject<Envelope>(json);

            StringAssert.Contains("\"base.gold\"", json);
            StringAssert.Contains("\"pet_vs_monster.gem\":7", json);
            Assert.AreEqual(BaseItemIds.Gold, loaded.Value);
            Assert.AreEqual(7, loaded.Quantities[ItemId.Parse("pet_vs_monster.gem")]);
        }
    }
}

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Hung.Base;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Base.Tests
{
    public sealed class AudioIdTests
    {
        private sealed class Envelope { public AudioId Sound; }
        [Serializable] private sealed class UnityEnvelope { public AudioId Sound; }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("base")]
        [TestCase("Base.UI")]
        public void Ctor_Invalid_Throws(string raw)
        {
            var ex = Assert.Throws<ArgumentException>(() => new AudioId(raw));
            StringAssert.Contains($"Invalid audio id: '{raw}'", ex.Message);
        }

        [Test]
        public void TryParse_ValidAndInvalid()
        {
            Assert.That(AudioId.TryParse("base.ui.click", out AudioId id), Is.True);
            Assert.That(id, Is.EqualTo(BaseAudioIds.UiClick));
            Assert.That(AudioId.TryParse("ui", out AudioId bad), Is.False);
            Assert.That(bad, Is.EqualTo(default(AudioId)));
        }

        [Test]
        public void Equality_IsOrdinalValueEquality()
        {
            Assert.That(AudioId.Parse("base.ui.click") == BaseAudioIds.UiClick, Is.True);
            Assert.That(AudioId.Parse("base.ui.click").GetHashCode(), Is.EqualTo(BaseAudioIds.UiClick.GetHashCode()));
            Assert.That(AudioId.Parse("base.ui.clicks") != BaseAudioIds.UiClick, Is.True);
            Assert.That(AudioId.Parse("a.b").CompareTo(AudioId.Parse("a.c")), Is.LessThan(0));
        }

        [Test]
        public void Default_IsInvalidAndEmpty()
        {
            Assert.That(default(AudioId).IsValid, Is.False);
            Assert.That(default(AudioId).ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Newtonsoft_RoundTrip()
        {
            string json = JsonConvert.SerializeObject(new Envelope { Sound = BaseAudioIds.SpinEnd });
            Assert.That(json, Is.EqualTo("{\"Sound\":\"base.spin.end\"}"));
            Assert.That(JsonConvert.DeserializeObject<Envelope>(json).Sound, Is.EqualTo(BaseAudioIds.SpinEnd));
        }

        [TestCase("{\"Sound\":5}", "Invalid AudioId token 'Integer'.")]
        [TestCase("{\"Sound\":\"Base.UI\"}", "Invalid AudioId value 'Base.UI'.")]
        public void Newtonsoft_Invalid_ThrowsMessage(string json, string message)
        {
            var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Envelope>(json));
            StringAssert.Contains(message, ex.Message);
        }

        [Test]
        public void Newtonsoft_NullToken_ThrowsTokenMessage()
        {
            var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Envelope>("{\"Sound\":null}"));
            StringAssert.Contains("Invalid AudioId token 'Null'.", ex.Message);
        }

        [Test]
        public void TypeConverter_RoundTrip()
        {
            TypeConverter c = TypeDescriptor.GetConverter(typeof(AudioId));
            Assert.That(c.ConvertFromString("base.ui.click"), Is.EqualTo(BaseAudioIds.UiClick));
            Assert.That(c.ConvertToString(BaseAudioIds.UiClick), Is.EqualTo("base.ui.click"));
        }

        [Test]
        public void JsonUtility_RoundTripKeepsValueField()
        {
            string json = JsonUtility.ToJson(new UnityEnvelope { Sound = BaseAudioIds.ItemClaim });
            Assert.That(json, Is.EqualTo("{\"Sound\":{\"value\":\"base.item.claim\"}}"));
            Assert.That(JsonUtility.FromJson<UnityEnvelope>(json).Sound, Is.EqualTo(BaseAudioIds.ItemClaim));
        }

        [Test]
        public void JsonUtility_EmptyValue_LoadsInvalidWithoutThrow()
        {
            UnityEnvelope loaded = JsonUtility.FromJson<UnityEnvelope>("{\"Sound\":{\"value\":\"\"}}");
            Assert.That(loaded.Sound.IsValid, Is.False);
        }

        [Test]
        public void BaseAudioIds_ExactSixValidUnique()
        {
            string[] values = typeof(BaseAudioIds).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(AudioId)).Select(f => ((AudioId)f.GetValue(null)).Value).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            Assert.That(values, Is.EqualTo(new[] { "base.item.claim", "base.spin.end", "base.spin.wheel", "base.ui.click", "base.ui.popup_hide", "base.ui.popup_show" }));
        }

        [Test]
        public void AudioId_IsStableId() => Assert.That(BaseAudioIds.UiClick, Is.InstanceOf<IStableId>());
    }
}

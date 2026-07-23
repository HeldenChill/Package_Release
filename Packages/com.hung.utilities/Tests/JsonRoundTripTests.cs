using System;
using NUnit.Framework;

namespace Hung.Utilities.Tests
{
    // JsonHelper (Runtime/ThirdParty/LitJson/JsonHelper.cs, global namespace) wraps JsonUtility, NOT the
    // LitJson library despite living in the LitJson folder -- confirmed this is what DataManager actually
    // calls. LitJson proper (namespace LitJson) is untouched third-party code, out of scope per ai-guardrails.
    public class JsonRoundTripTests
    {
        [Serializable]
        private class Poco
        {
            public int id;
            public string label;
        }

        [Test]
        public void Json_Serialize_Deserialize_Equal()
        {
            var original = new Poco { id = 7, label = "seven" };

            string json = JsonHelper.ItemToJson(original, false);
            Poco result = JsonHelper.ItemFromJson<Poco>(json);

            Assert.AreEqual(original.id, result.id);
            Assert.AreEqual(original.label, result.label);
        }

        [Test]
        public void Json_MissingField_DefaultsNotThrows()
        {
            // Hand-crafted wrapper JSON missing "label" entirely.
            string json = "{\"Item\":{\"id\":3}}";

            Poco result = JsonHelper.ItemFromJson<Poco>(json);

            Assert.AreEqual(3, result.id);
            Assert.IsNull(result.label);
        }
    }
}

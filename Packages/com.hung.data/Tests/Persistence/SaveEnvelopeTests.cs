using System;
using System.Text;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class SaveEnvelopeTests
    {
        [Test]
        public void Envelope_RoundTripsAndAuthenticatesMetadata()
        {
            var payload = new SaveEncodedPayload("json", Encoding.UTF8.GetBytes("{\"value\":7}"));
            SaveEnvelope envelope = SaveEnvelope.Create("game-data", 1, new DateTime(2026, 7, 23, 1, 2, 3, DateTimeKind.Utc), payload, "sha256");
            envelope.Integrity = "tag";

            SaveEnvelope loaded = SaveEnvelope.FromJson(envelope.ToJson());

            Assert.That(loaded.FormatVersion, Is.EqualTo(1));
            Assert.That(JObject.Parse(loaded.ToJson())["saveKey"].Value<string>(), Is.EqualTo("game-data"));
            CollectionAssert.AreEqual(envelope.GetAuthenticatedBytes(), loaded.GetAuthenticatedBytes());
            loaded.PayloadSchemaVersion = 2;
            CollectionAssert.AreNotEqual(envelope.GetAuthenticatedBytes(), loaded.GetAuthenticatedBytes());
        }

        [Test]
        public void Envelope_RejectsMismatchedOrNewerKey()
        {
            SaveEnvelope envelope = SaveEnvelope.Create("game-data", 1, DateTime.UtcNow, new SaveEncodedPayload("json", Encoding.UTF8.GetBytes("{}")), "sha256");

            Assert.That(envelope.ValidateFor("other").DiagnosticCode, Is.EqualTo("SAVE_KEY_MISMATCH"));

            envelope.FormatVersion = 99;
            Assert.That(envelope.ValidateFor("game-data").DiagnosticCode, Is.EqualTo("SAVE_ENVELOPE_NEWER_THAN_CLIENT"));
        }
    }
}

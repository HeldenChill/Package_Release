using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hung.Base.Persistence;
using NUnit.Framework;

namespace Hung.Persistence.Tests.DataComposition
{
    public sealed class DefinitionSetTests
    {
        private static void Reject(string prefix, params SaveDefinition[] definitions)
        {
            var error = Assert.Throws<ArgumentException>(() => SaveDefinitionSet.Validate(definitions));
            StringAssert.StartsWith(prefix, error.Message);
        }
        [Test] public void EmptyAndDisjointSetsAreValid()
        {
            SaveDefinitionSet.Validate(Array.Empty<SaveDefinition>());
            SaveDefinitionSet.Validate(new SaveDefinition[] { Definitions.Make<Wallet>("wallet"), Definitions.Make<Preferences>("prefs") });
        }
        [Test] public void NullSequenceAndEntryAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => SaveDefinitionSet.Validate(null));
            Assert.Throws<ArgumentException>(() => SaveDefinitionSet.Validate(new SaveDefinition[] { null }));
        }
        [Test] public void DuplicateModelAndRepeatedInstanceAreRejected()
        {
            var first = Definitions.Make<Wallet>("wallet");
            Reject("SAVE_DEFINITION_MODEL_COLLISION", first, Definitions.Make<Wallet>("other"));
            Reject("SAVE_DEFINITION_MODEL_COLLISION", first, first);
        }
        [Test] public void ExactCanonicalCollisionNamesTheConflictingKey()
        {
            var error = Assert.Throws<ArgumentException>(() => SaveDefinitionSet.Validate(new SaveDefinition[] { Definitions.Make<Wallet>("shared"), Definitions.Make<Preferences>("shared") }));
            StringAssert.StartsWith("SAVE_DEFINITION_NAME_COLLISION", error.Message);
            StringAssert.Contains("shared", error.Message);
            StringAssert.Contains(typeof(Wallet).FullName, error.Message);
            StringAssert.Contains(typeof(Preferences).FullName, error.Message);
        }
        [Test] public void CanonicalAliasCollisionFailsInBothOrders()
        {
            var wallet = Definitions.Make<Wallet>("wallet", new[] { "old" });
            var prefs = Definitions.Make<Preferences>("old");
            Reject("SAVE_DEFINITION_NAME_COLLISION", wallet, prefs);
            Reject("SAVE_DEFINITION_NAME_COLLISION", prefs, wallet);
        }
        [Test] public void AliasAliasCollisionIsRejected()
        {
            Reject("SAVE_DEFINITION_NAME_COLLISION", Definitions.Make<Wallet>("wallet", new[] { "old" }), Definitions.Make<Preferences>("prefs", new[] { "old" }));
        }
        [Test] public void CanonicalCaseCollisionIsRejectedWithoutChangingKeys()
        {
            var wallet = Definitions.Make<Wallet>("Wallet");
            var prefs = Definitions.Make<Preferences>("wallet");
            Reject("SAVE_DEFINITION_FILE_COLLISION", wallet, prefs);
            Assert.That(wallet.Key, Is.EqualTo("Wallet"));
            Assert.That(prefs.Key, Is.EqualTo("wallet"));
        }
        [Test] public void OwnKeyAliasAndCaseDistinctAliasesAreValid()
        {
            SaveDefinitionSet.Validate(new SaveDefinition[] { Definitions.Make<Wallet>("wallet", new[] { "wallet", "OLD" }), Definitions.Make<Preferences>("prefs", new[] { "old" }) });
        }
        [Test] public void MetadataValidationDoesNotInvokeModelDefaults()
        {
            int calls = 0;
            var noWork = new NoWork();
            var definition = new SaveDefinition<Wallet>("wallet", 1, () => { calls++; throw new Exception("must not load"); },
                _ => { throw new Exception("must not validate values"); }, Array.Empty<ISaveMigration>(), Array.Empty<string>(), noWork, noWork, SaveFailurePolicy.FailClosed);
            SaveDefinitionSet.Validate(new SaveDefinition[] { definition });
            Assert.That(calls, Is.Zero);
        }
        [Test] public void InputIsEnumeratedOnce()
        {
            SaveDefinitionSet.Validate(new Once(new SaveDefinition[] { Definitions.Make<Wallet>("wallet") }));
        }
        private sealed class Once : IEnumerable<SaveDefinition>
        {
            private readonly IEnumerable<SaveDefinition> values;
            private bool read;
            public Once(IEnumerable<SaveDefinition> values) { this.values = values; }
            public IEnumerator<SaveDefinition> GetEnumerator() { Assert.That(read, Is.False); read = true; return values.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        private sealed class NoWork : ISaveCodec, ISaveProtector
        {
            public string EncodingId => throw new Exception("must not query codec");
            public string ProtectionId => throw new Exception("must not query protector");
            public SaveEncodedPayload Encode(byte[] bytes) => throw new Exception("must not encode");
            public byte[] Decode(SaveEncodedPayload payload) => throw new Exception("must not decode");
            public string Protect(byte[] bytes) => throw new Exception("must not protect");
            public bool Verify(byte[] bytes, string tag) => throw new Exception("must not verify");
        }
    }
}

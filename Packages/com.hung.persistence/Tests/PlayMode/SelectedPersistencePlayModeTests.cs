using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Persistence.PlayModeTests
{
    public sealed class SelectedPersistencePlayModeTests
    {
        private static int entries;
        public sealed class State { public int Balance; public List<string> Receipts = new List<string>(); }
        [UnityTest] public IEnumerator IsolatedSelectedGraphRoundTripsBalanceAndReceiptAcrossNewService()
        {
            int entry = ++entries;
            string root = Path.Combine(Path.GetTempPath(), "HungDataPlanPlay_" + Guid.NewGuid().ToString("N"));
            try
            {
                var codec = new PlainJsonSaveCodec(); var protector = new Sha256SaveProtector();
                var definition = new SaveDefinition<State>("fixture-play", 1, () => new State(), _ => SaveValidationResult.Valid(),
                    Array.Empty<ISaveMigration>(), Array.Empty<string>(), codec, protector, SaveFailurePolicy.FailClosed);
                SaveDefinitionSet.Validate(new SaveDefinition[] { definition });
                var first = new PersistenceBuilder().WithRoot(root).WithCodec(codec).WithProtector(protector).Register(definition).Build();
                Assert.That(first.Save(definition, new State { Balance = 8, Receipts = { "fixture.claim" } }).Success, Is.True);
                yield return null;
                var second = new PersistenceBuilder().WithRoot(root).WithCodec(codec).WithProtector(protector).Register(definition).Build();
                Assert.That(second, Is.Not.SameAs(first));
                var loaded = second.Load(definition);
                Assert.That(loaded.Success, Is.True);
                Assert.That(loaded.Source, Is.EqualTo(SaveDataSource.Primary));
                Assert.That(loaded.Value.Balance, Is.EqualTo(8));
                Assert.That(loaded.Value.Receipts, Is.EqualTo(new[] { "fixture.claim" }));
                Debug.Log("DATA_SELECTED_PLAY entry=" + entry + " root=" + root + " balance=8 receipts=1 new_service=true");
            }
            finally
            {
                string full = Path.GetFullPath(root);
                string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("HungDataPlanPlay_", StringComparison.Ordinal))
                    throw new InvalidOperationException("Refusing cleanup outside the owned test root.");
                if (Directory.Exists(full)) Directory.Delete(full, true);
            }
        }
    }
}

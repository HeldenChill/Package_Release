using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class SaveMigrationRegistryTests
    {
        [Test]
        public void Registry_RunsContinuousChain()
        {
            var registry = new SaveMigrationRegistry(new ISaveMigration[] { new SetMigration(0, 1, "a"), new SetMigration(1, 2, "b") });

            SaveMigrationResult result = registry.Migrate(new JObject(), 0, 2);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Value["a"].Value<bool>(), Is.True);
            Assert.That(result.Value["b"].Value<bool>(), Is.True);
        }

        [Test]
        public void Registry_RejectsDuplicateGapAndBadStep()
        {
            Assert.Throws<ArgumentException>(() => new SaveMigrationRegistry(new ISaveMigration[] { new SetMigration(1, 1, "x") }));
            Assert.Throws<ArgumentException>(() => new SaveMigrationRegistry(new ISaveMigration[] { new SetMigration(1, 2, "x"), new SetMigration(1, 2, "y") }));

            var registry = new SaveMigrationRegistry(new ISaveMigration[] { new SetMigration(1, 2, "x") });
            Assert.That(registry.Migrate(new JObject(), 0, 2).ErrorCode, Is.EqualTo("SAVE_MIGRATION_CHAIN_MISSING"));
            Assert.That(registry.Migrate(new JObject(), 3, 2).ErrorCode, Is.EqualTo("SAVE_SCHEMA_NEWER_THAN_CLIENT"));
        }

        private sealed class SetMigration : ISaveMigration
        {
            private readonly string field;
            public SetMigration(int from, int to, string field)
            {
                FromVersion = from;
                ToVersion = to;
                this.field = field;
            }

            public int FromVersion { get; }
            public int ToVersion { get; }
            public JObject Migrate(JObject source)
            {
                source[field] = true;
                return source;
            }
        }
    }
}

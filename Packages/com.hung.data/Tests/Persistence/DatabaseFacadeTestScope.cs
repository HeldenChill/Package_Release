using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;

namespace Hung.Data.Tests.Persistence
{
    internal sealed class DatabaseFacadeTestScope : IDisposable
    {
        private readonly Func<IPersistenceService> previousServiceFactory;
        private readonly ICompatibilitySaveDefinitionFactory previousDefinitionFactory;

        public DatabaseFacadeTestScope(ILegacySaveSource legacy = null)
        {
            previousServiceFactory = Database.ServiceFactory;
            previousDefinitionFactory = Database.CompatibilityDefinitionFactory;

            Store = new InMemorySaveStore();
            Service = new PersistenceService(Store, legacy);
            var codec = new PlainJsonSaveCodec();
            var protector = new Sha256SaveProtector();
            PackageSaveDefinitions.RegisterAll(Service, codec, protector);

            Database.CompatibilityDefinitionFactory = new CompatibilitySaveDefinitionFactory(codec, protector);
            Database.ServiceFactory = () => Service;
        }

        public InMemorySaveStore Store { get; }
        public PersistenceService Service { get; }

        public void Dispose()
        {
            Database.CompatibilityDefinitionFactory = previousDefinitionFactory;
            Database.ServiceFactory = previousServiceFactory;
        }
    }
}

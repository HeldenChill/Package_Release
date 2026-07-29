using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class PersistenceBootstrapTests
    {
        [Test]
        public void InstallDefault_DoesNotOverwriteExplicitServiceFactory()
        {
            Func<IPersistenceService> previous = Database.ServiceFactory;
            var service = new PersistenceService(new InMemorySaveStore());
            Func<IPersistenceService> explicitFactory = () => service;
            try
            {
                Database.ServiceFactory = explicitFactory;

                PersistenceBootstrap.InstallDefault();

                Assert.That(Database.ServiceFactory, Is.SameAs(explicitFactory));
            }
            finally
            {
                Database.ServiceFactory = previous;
            }
        }
    }
}

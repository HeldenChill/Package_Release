using System;
using Hung.Base.Composition;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hung.Data.Tests.Persistence
{
    public sealed class AutomaticPersistenceGuardTests
    {
        [Test] public void DisabledDefault_DoesNotInstallFactoryOrConstructStore()
        {
            var old = Database.ServiceFactory;
            try
            {
                using (var profile = new ProfileScope())
                {
                    Database.ServiceFactory = null;
                    PersistenceBootstrap.InstallDefault();
                    Assert.That(Database.ServiceFactory, Is.Null);
                    Func<IPersistenceService> sentinel = () => throw new InvalidOperationException("must not invoke");
                    Database.ServiceFactory = sentinel;
                    PersistenceBootstrap.InstallDefault();
                    Assert.That(Database.ServiceFactory, Is.SameAs(sentinel));
                }
            }
            finally { Database.ServiceFactory = old; }
        }

        private sealed class ProfileScope : IDisposable
        {
            private const string Root = "Assets/P2CStartupGuardTest";
            public ProfileScope()
            {
                if (Resources.Load<AutomaticStartupProfile>(AutomaticStartupPolicy.ResourcePath) != null || AssetDatabase.IsValidFolder(Root))
                    throw new InvalidOperationException("P2C_TEST_MUST_NOT_REPLACE_EXISTING_PROFILE");
                AssetDatabase.CreateFolder("Assets", "P2CStartupGuardTest");
                AssetDatabase.CreateFolder(Root, "Resources");
                var p = ScriptableObject.CreateInstance<AutomaticStartupProfile>();
                p.legacyPersistence = false; p.legacyAnalytics = false;
                AssetDatabase.CreateAsset(p, Root + "/Resources/HungAutomaticStartup.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (AutomaticStartupPolicy.ShouldStart(AutomaticStartupKind.LegacyPersistence))
                    throw new InvalidOperationException("P2C_TEST_PROFILE_NOT_VISIBLE");
            }
            public void Dispose()
            {
                if (!AssetDatabase.DeleteAsset(Root))
                    throw new InvalidOperationException("P2C_TEST_PROFILE_CLEANUP_FAILED");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

    }
}

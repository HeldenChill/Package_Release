using System;
using System.IO;
using Hung.Base.Persistence;
using UnityEngine;

namespace Hung.Data.Persistence
{
    public sealed class CompatibilitySaveDefinitionFactory : ICompatibilitySaveDefinitionFactory
    {
        private readonly ISaveCodec codec;
        private readonly ISaveProtector protector;
        private readonly ISaveDiagnostics diagnostics;

        public CompatibilitySaveDefinitionFactory(
            ISaveCodec codec,
            ISaveProtector protector,
            ISaveDiagnostics diagnostics = null)
        {
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
            this.protector = protector ?? throw new ArgumentNullException(nameof(protector));
            this.diagnostics = diagnostics ?? NullSaveDiagnostics.Instance;
        }

        public SaveDefinition<T> Create<T>(string key) where T : new()
        {
            diagnostics.Report(new SaveDiagnostic(
                "SAVE_UNREGISTERED_COMPATIBILITY_DEFINITION",
                key,
                SaveDataSource.None,
                SaveRecoveryState.None));

            return new SaveDefinition<T>(
                key,
                1,
                () => new T(),
                value => value == null
                    ? SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED")
                    : SaveValidationResult.Valid(),
                Array.Empty<ISaveMigration>(),
                new[] { key },
                codec,
                protector,
                SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);
        }
    }

    public sealed class UnityDebugSaveDiagnostics : ISaveDiagnostics
    {
        public void Report(SaveDiagnostic diagnostic)
        {
            Debug.LogWarning($"[{diagnostic.Code}] save={diagnostic.SaveKey} source={diagnostic.Source} recovery={diagnostic.Recovery} evidence={diagnostic.EvidencePath}");
        }
    }

    public static class PersistenceBootstrap
    {
        private const string KeyPurpose = "persistence-hmac-v1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InstallDefault()
        {
            if (Database.ServiceFactory == null)
                Database.ServiceFactory = CreateDefault;
        }

        public static IPersistenceService CreateDefault()
        {
            string comHungRoot = Path.Combine(Application.persistentDataPath, "ComHung");
            var keyProvider = new LocalSecretKeyProvider(Path.Combine(comHungRoot, "Keys"));
            SaveSecretKeyResult keyResult = keyProvider.GetOrCreateKey(KeyPurpose);
            if (!keyResult.Success)
                throw new PersistenceException(keyResult.ErrorCode);

            var codec = new BeneficialCompressionCodec(16 * 1024);
            var protector = new HmacSha256SaveProtector(keyResult.Key);
            var diagnostics = new UnityDebugSaveDiagnostics();
            var service = new PersistenceService(
                new FileSaveStore(Path.Combine(comHungRoot, "Saves")),
                new PlayerPrefsLegacySaveSource(),
                diagnostics);

            PackageSaveDefinitions.RegisterAll(service, codec, protector);
            Database.CompatibilityDefinitionFactory = new CompatibilitySaveDefinitionFactory(codec, protector, diagnostics);
            return service;
        }
    }
}

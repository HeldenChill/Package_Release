using System;
using System.Collections.Generic;
using Hung.Base.Persistence;

namespace Hung.Data.Persistence
{
    /// <summary>
    /// Explicit composition root for a <see cref="PersistenceService"/> graph: store root,
    /// file operations, codec, protector, diagnostics, and optional legacy/evidence ports.
    /// Paths and key material are supplied by the caller - this type never reads
    /// a platform persistent-data path itself. Late or missing composition fails with an
    /// actionable exception, never a silent fallback.
    /// </summary>
    public sealed class PersistenceBuilder
    {
        private string root;
        private IFileSaveOperations operations;
        private ISaveCodec codec;
        private ISaveProtector protector;
        private ISaveDiagnostics diagnostics;
        private ILegacySaveSource legacy;
        private ICanonicalEvidenceStore evidence;
        private bool protectorConfigured;
        private readonly List<Action<PersistenceService>> registrations = new List<Action<PersistenceService>>();
        private readonly HashSet<string> registeredKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<Type> registeredModelTypes = new HashSet<Type>();

        public PersistenceBuilder WithRoot(string rootPath)
        {
            root = string.IsNullOrWhiteSpace(rootPath)
                ? throw new ArgumentException("Persistence store root path cannot be empty.", nameof(rootPath))
                : rootPath;
            return this;
        }

        public PersistenceBuilder WithFileOperations(IFileSaveOperations fileOperations)
        {
            operations = fileOperations ?? throw new ArgumentNullException(nameof(fileOperations));
            return this;
        }

        public PersistenceBuilder WithCodec(ISaveCodec saveCodec)
        {
            codec = saveCodec ?? throw new ArgumentNullException(nameof(saveCodec));
            return this;
        }

        /// <summary>
        /// Resolves a key from <paramref name="keyProvider"/> for <paramref name="keyPurpose"/>
        /// and constructs an <see cref="HmacSha256SaveProtector"/> from it. Mutually exclusive
        /// with <see cref="WithProtector"/> - calling both is an actionable error.
        /// </summary>
        public PersistenceBuilder WithHmacProtection(ISecretKeyProvider keyProvider, string keyPurpose)
        {
            if (keyProvider == null)
                throw new ArgumentNullException(nameof(keyProvider));
            if (protectorConfigured)
                throw new InvalidOperationException(
                    "Persistence protector already configured. Call only one of WithHmacProtection or WithProtector.");

            SaveSecretKeyResult keyResult = keyProvider.GetOrCreateKey(keyPurpose);
            if (!keyResult.Success)
                throw new PersistenceException(keyResult.ErrorCode);

            protector = new HmacSha256SaveProtector(keyResult.Key);
            protectorConfigured = true;
            return this;
        }

        /// <summary>
        /// Supplies an already-constructed protector that needs no key material (for example
        /// <see cref="Sha256SaveProtector"/>). Mutually exclusive with
        /// <see cref="WithHmacProtection"/>.
        /// </summary>
        public PersistenceBuilder WithProtector(ISaveProtector saveProtector)
        {
            if (saveProtector == null)
                throw new ArgumentNullException(nameof(saveProtector));
            if (protectorConfigured)
                throw new InvalidOperationException(
                    "Persistence protector already configured. Call only one of WithHmacProtection or WithProtector.");

            protector = saveProtector;
            protectorConfigured = true;
            return this;
        }

        /// <summary>
        /// The protector resolved by <see cref="WithHmacProtection"/> or <see cref="WithProtector"/>,
        /// so callers that also compose definitions outside the builder (for example a product's
        /// own <c>RegisterAll(service, codec, protector)</c> step) can reuse the same instance
        /// without re-deriving it.
        /// </summary>
        public ISaveProtector Protector => protector;

        /// <summary>The codec configured via <see cref="WithCodec"/>, exposed for the same reason as <see cref="Protector"/>.</summary>
        public ISaveCodec Codec => codec;

        public PersistenceBuilder WithDiagnostics(ISaveDiagnostics saveDiagnostics)
        {
            diagnostics = saveDiagnostics ?? throw new ArgumentNullException(nameof(saveDiagnostics));
            return this;
        }

        public PersistenceBuilder WithLegacySource(ILegacySaveSource legacySource)
        {
            legacy = legacySource ?? throw new ArgumentNullException(nameof(legacySource));
            return this;
        }

        public PersistenceBuilder WithEvidenceStore(ICanonicalEvidenceStore evidenceStore)
        {
            evidence = evidenceStore ?? throw new ArgumentNullException(nameof(evidenceStore));
            return this;
        }

        /// <summary>
        /// Registers a product definition. Duplicate keys and duplicate model types are
        /// actionable errors, never a silent overwrite - see
        /// <see cref="Hung.Base.Persistence.SaveDefinition"/>'s typeof(T)-keyed registry.
        /// </summary>
        public PersistenceBuilder Register<T>(SaveDefinition<T> definition) where T : new()
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (!registeredKeys.Add(definition.Key))
                throw new InvalidOperationException($"Persistence definition key '{definition.Key}' is already registered.");
            if (!registeredModelTypes.Add(typeof(T)))
                throw new InvalidOperationException($"Persistence definition model type '{typeof(T)}' is already registered.");

            registrations.Add(service => service.Register(definition));
            return this;
        }

        public PersistenceService Build()
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Persistence store root is not configured. Call WithRoot before Build.");
            if (codec == null)
                throw new InvalidOperationException("Persistence codec is not configured. Call WithCodec before Build.");
            if (!protectorConfigured)
                throw new InvalidOperationException("Persistence protector is not configured. Call WithHmacProtection or WithProtector before Build.");

            ISaveDiagnostics resolvedDiagnostics = diagnostics ?? NullSaveDiagnostics.Instance;
            var store = new FileSaveStore(root, operations);
            var service = new PersistenceService(store, legacy, resolvedDiagnostics, evidence);

            foreach (Action<PersistenceService> register in registrations)
                register(service);

            return service;
        }
    }
}

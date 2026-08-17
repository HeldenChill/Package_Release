# Hung Persistence (`com.hung.persistence`)

Generic local save persistence. Layer 1 — **mechanism only**.

This package owns *how bytes are stored safely*. It owns no game data and calls no Unity API.
Its asmdef references are deliberately empty: paths, key material, and Unity primitives are
injected into it, never called from it.

## Boundary

| This package holds | This package never holds |
|---|---|
| Save definitions, envelope, migration registry | Concrete product models |
| Atomic file store, primary/backup rotation, quarantine | Currency, wallet, or feature semantics |
| Codecs, HMAC protection, secret-key ports | `UnityEngine` / `UnityEditor` / `Application.*` / `PlayerPrefs` |

Product definitions live in the consuming package. See
`.cursor/plans/tier3-data-ownership-boundary-design.md` for the three-layer ownership rule.

## Public API index

### Contracts (`Runtime/Contracts/`)

| Type | File | Role |
|---|---|---|
| `IPersistenceService` | `IPersistenceService.cs` | Save/load entry point |
| `ICompatibilitySaveDefinitionFactory` | `IPersistenceService.cs` | Compatibility definition lookup |
| `ISaveStore` | `ISaveStore.cs` | Byte-level store port |
| `ISaveDiagnostics`, `NullSaveDiagnostics` | `ISaveDiagnostics.cs` | Diagnostic sink |
| `ILegacySaveSource` | `ILegacySaveSource.cs` | Import-only port from a retired medium |
| `SaveDefinition`, `SaveDefinition<T>` | `SaveDefinition.cs` | Key, schema, validator, migration wiring |
| `SaveFailurePolicy`, `SaveDataSource`, `SaveRecoveryState` | `PersistenceResults.cs` | Result enums |
| `SaveStoreReadResult`, `SaveStoreWriteResult`, `SaveResult`, `LoadResult<T>` | `PersistenceResults.cs` | Result types |
| `SaveDiagnostic`, `PersistenceException` | `PersistenceResults.cs` | Diagnostics and failure |
| `SaveValidationResult`, `SaveEncodedPayload`, `SaveSecretKeyResult` | `SaveTransforms.cs` | Transform results |
| `ISaveCodec`, `ISaveProtector`, `ISecretKeyProvider`, `ISaveMigration` | `SaveTransforms.cs` | Transform ports |
| `ICanonicalEvidenceStore` | `ICanonicalEvidenceStore.cs` | Canonical-existence receipt port (crash-safety, D4) |

### Implementation (`Runtime/Implementation/`)

| Type | File | Role |
|---|---|---|
| `PersistenceService` | `PersistenceService.cs` | Load order, recovery, legacy import |
| `FileSaveStore` | `FileSaveStore.cs` | Atomic temp to primary write, backup rotation, quarantine |
| `IFileSaveOperations`, `SystemFileSaveOperations` | `IFileSaveOperations.cs` | Filesystem port |
| `SaveEnvelope` | `SaveEnvelope.cs` | Versioned payload envelope |
| `SaveMigrationResult`, `SaveMigrationRegistry`, `LegacyRawJsonToSchemaOneMigration` | `SaveMigrationRegistry.cs` | Schema migration chain |
| `PlainJsonSaveCodec`, `GZipSaveCodec`, `BeneficialCompressionCodec` | `SaveCodecs.cs` | Payload codecs |
| `Sha256SaveProtector`, `HmacSha256SaveProtector` | `SaveProtectors.cs` | Integrity protection |
| `LocalSecretKeyProvider` | `LocalSecretKeyProvider.cs` | Local key derivation |
| `FileLegacySaveSource` | `FileLegacySaveSource.cs` | File-backed import-only legacy source |
| `FileCanonicalEvidenceStore` | `FileCanonicalEvidenceStore.cs` | File-backed canonical-existence receipts |
| `OwnedRootLayout` | `OwnedRootLayout.cs` | Read-only enumeration of the owned root's subdirectories, for product-side reset |

### Composition (`Runtime/Composition/`)

| Type | File | Role |
|---|---|---|
| `PersistenceBuilder` | `PersistenceBuilder.cs` | Fluent composition of root, codec, protector, and definitions into a `PersistenceService` |

## Namespaces

Types keep their pre-extraction namespaces (`Hung.Base.Persistence`, `Hung.Data.Persistence`).
Assembly name and namespace intentionally differ, so the extraction required no consumer
`using` changes. Namespace renames are a separate, later decision.

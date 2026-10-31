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
| `SaveDefinitionSet` | `SaveDefinitionSet.cs` | Metadata-only selected-definition ownership preflight |
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

## Horizon 1 explicit Data composition

Use `com.hung.persistence` directly for product-owned models. Its `Hung.Base.Persistence`
and `Hung.Data.Persistence` namespaces do not require the Base or Data packages.
`SaveDefinitionSet.Validate(IEnumerable<SaveDefinition>)` is the public metadata-only preflight:
one model owner, ordinal canonical/alias ownership, and case-insensitive canonical filename
uniqueness per graph. Empty sets and an alias equal to its own canonical key are valid.
Errors identify `SAVE_DEFINITION_MODEL_COLLISION`, `SAVE_DEFINITION_NAME_COLLISION` or
`SAVE_DEFINITION_FILE_COLLISION`. The validator invokes no default, codec, protector or store.

`PersistenceBuilder.Register<T>` rejects conflicts atomically through the same rules and
keeps its existing InvalidOperationException contract. `Build` revalidates before store
construction. Direct `PersistenceService.Register<T>` retains legacy replacement behavior;
post-build registration and direct operations with unregistered definitions bypass this strict route.

```csharp
using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;

public sealed class ProductPreferences { public bool Muted; }
public static class ProductPersistence
{
    public static IPersistenceService Compose(string root, ISaveCodec codec, ISaveProtector protector)
    {
        var preferences = new SaveDefinition<ProductPreferences>("product.preferences", 1,
            () => new ProductPreferences(), value => value != null
                ? SaveValidationResult.Valid() : SaveValidationResult.Invalid("PRODUCT_INVALID_PREFERENCES"),
            Array.Empty<ISaveMigration>(), Array.Empty<string>(), codec, protector, SaveFailurePolicy.FailClosed);
        SaveDefinitionSet.Validate(new SaveDefinition[] { preferences });
        return new PersistenceBuilder().WithRoot(root).WithCodec(codec).WithProtector(protector)
            .Register(preferences).Build();
    }
}
```

The caller supplies protection policy; the example makes no production security choice.
`WithHmacProtection` resolves its key immediately, so validation cannot undo earlier key I/O.
Construct the graph inside the selected product state feature's initializer; use the existing
FeatureHost when needed. An unselected feature never invokes its composition delegate.
Load/validate/normalize and commit required changes before owner readiness. Keep balances
and grant receipts in one durable commit. A failed import/schema-rewrite commit returns failure
with its write diagnostic and Unrecoverable; it does not publish migrated success or quarantine
the valid source merely because its rewrite failed. Retry only after addressing the write failure.
This does not claim every underlying store failure rolls back physical bytes.

Legacy DataManager, IDataService, Database, PackageSaveDefinitions.CreateAll/RegisterAll and
the default Data bootstrap remain compatibility APIs. Individual feature definition factories
can be selected explicitly. The all-definition methods register the legacy aggregate plus bundled
features and are not the independent-product route. If installed, suppress the legacy callback
with the existing product legacyPersistence policy before it runs. An absent profile still enables
legacy factory installation. Do not run two writable owners against one account.

No product schema, root, key purpose, game-data aliases, profile or serialized identity changes
in this wave. Typed JSON does not guarantee unknown-member retention; products needing that
must supply explicit extension-data/converter fixtures. TemplateGame Data integration and actual
consumer/save adoption are deferred until Horizon 1 planning is complete. Audio clip restoration
and BW8 resume remain separate. Package fixtures are not product/player acceptance evidence.

### Backup recovery review repair (2026-10-28)

Backup decoding/validation and migration are separate from persistence writes. Recovery restores
validated backup bytes to primary and checks the restore result before committing a required schema
rewrite. A failed restore/rewrite returns failure instead of reporting readiness; it does not rotate a
corrupt primary over the valid backup or restore stale bytes over the migrated primary. Real-file-store
regressions cover corrupt/missing primary, successful schema rewrite and fresh-service restart, restore
failure, and failed promotion with retry. General rollback across every file failure is not guaranteed.
Evidence: `Docs/reports/horizon1-boundaries/service-architecture/data-composition/review/fixes/README.md`.

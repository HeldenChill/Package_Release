# Hung Data (L2)

Data framework: SO definitions, save, DataManager. Second assembly: Hung.Common - holds only `ConditionBarrier.cs` (its former `SStats.Stat`/`StatModifier`/`StatModType` duplicate was deleted 2026-07-11, Hung.Base is now sole owner). Kept as a separate assembly deliberately (Ph3 Task 3.3, 2026-07-11): dissolving it into Hung.Data would mean repointing 8 consumer asmdefs for zero functional gain - not worth the churn for one file.

## DailyGift boundary contract

`Hung.Data.LiveOps.IDailyGiftConfig`, `IDailyGiftDay`, and `IDailyGiftReward` describe DailyGift configuration without shipping a product ScriptableObject or reward model. Product assemblies retain their own serialized config asset and implement these interfaces. `DailyGiftDataSO`, `DailyGiftItem`, and `Runtime/CharacterLegacy` were removed in `0.11.0` because they were PVM-owned identities.

## Public API index

- `IDailyGiftConfig` — product-owned DailyGift configuration boundary.
- `IDailyGiftDay` — one zero-based DailyGift day and rewards.
- `IDailyGiftReward` — stable `ItemId` and quantity reward record.

## Item Catalog Authoring

Create game items in `ItemCatalog.asset` with stable `ItemId` values. Base IDs use the `base.` namespace; game IDs should use a game namespace, for example `pet_vs_monster.gem`.

Run item catalog validation before release. Duplicate IDs, duplicate code names, null definitions, and invalid identifiers fail validation. Rename IDs only with a save/content migration plan; IDs are serialized keys.

Run the ItemId code generator after catalog changes. It writes deterministic game constants, skipping `base.` entries. Generated constants are convenience wrappers; runtime lookup still uses catalog data.

ItemId Inspector fields use a cached, namespace-grouped Unity dropdown with built-in search and keyboard navigation. Search matches the complete raw ID. Unknown IDs remain visible with a warning instead of being cleared; when no catalogs are available, the field falls back to validated text entry.

Unity 6 enum fields inspected through Odin use a local Unity-native compatibility drawer for ordinary and flags enums. Keep this bridge until an Odin upgrade is verified against the project's Unity version and passes the enum popup regression checks.

## Persistence Composition

The default bootstrap writes canonical saves beneath `Application.persistentDataPath/ComHung/Saves` and keeps the HMAC key separately beneath `Application.persistentDataPath/ComHung/Keys`:

```csharp
// Installed automatically before scene load. Call explicitly only when code
// must use Database earlier than Unity runtime initialization.
PersistenceBootstrap.InstallDefault();
```

Manual and test composition stays explicit:

```csharp
var codec = new PlainJsonSaveCodec();
var protector = new Sha256SaveProtector(); // deterministic corruption check for tests
var service = new PersistenceService(testStore, testLegacySource, testDiagnostics);
PackageSaveDefinitions.RegisterAll(service, codec, protector);

Database.CompatibilityDefinitionFactory =
    new CompatibilitySaveDefinitionFactory(codec, protector, testDiagnostics);
Database.ServiceFactory = () => service;
```

Production composition uses `BeneficialCompressionCodec`, `HmacSha256SaveProtector`, `LocalSecretKeyProvider`, `FileSaveStore`, and `PlayerPrefsLegacySaveSource`. Package internals should depend directly on `IPersistenceService`; static `Database` remains for existing callers.

See `Docs/PERSISTENCE_MIGRATION.md` for key mappings, recovery evidence, and rollback.

Known debt:
- Runtime/ contains a Hung.Base.asmref (asmref-injection pattern) - retirement planned per master-project vision.

## Explicit selected persistence (Horizon 1 P2-A, unreleased)

New product composition can use the existing PersistenceBuilder without the
Database facade, framework GameData or PackageSaveDefinitions.RegisterAll.
Register only selected definitions before Build, retain the typed definitions,
and use IPersistenceService.Load/Save explicitly. A pool-only host needs no save
service. Do not create another save framework or universal aggregate.

The implementation lives in com.hung.persistence; its existing composition types
use the Hung.Data.Persistence namespace. This single-definition product example
uses caller-supplied storage/protection policy and does not select LiveOps:

```csharp
using System;
using Hung.Base.Persistence;
using Hung.Data.Persistence;

public static class ProductPersistenceCompositionExample
{
    public sealed class ProductState
    {
        public bool IsBgmMuted;
        public bool IsSfxMuted;
    }

    public static (IPersistenceService Service, SaveDefinition<ProductState> Definition)
        Compose(string productSaveRoot, ISaveCodec codec, ISaveProtector protector)
    {
        var definition = new SaveDefinition<ProductState>("example-product", 1,
            () => new ProductState(),
            value => value == null
                ? SaveValidationResult.Invalid("PRODUCT_STATE_MISSING")
                : SaveValidationResult.Valid(),
            Array.Empty<ISaveMigration>(), Array.Empty<string>(), codec, protector,
            SaveFailurePolicy.FailClosed);
        var service = new PersistenceBuilder().WithRoot(productSaveRoot)
            .WithCodec(codec).WithProtector(protector).Register(definition).Build();
        return (service, definition);
    }
}
```

For multiple definitions, product preflight must reject duplicate model types,
canonical keys and cross-owner alias/canonical-name collisions before key or
storage side effects. The builder checks canonical keys/types; it does not check
alias ownership. Direct PersistenceService.Register overwrites by model type and
must not substitute for selected composition validation. SaveDefinition already
rejects duplicate aliases within a single definition; the product may allow its
own canonical name as its own alias. See ExplicitFeatureCompositionTests for the
complete preflight and isolated storage examples.

Load/normalization, first-run initialization and session resets are separate
product actions. Preserve existing values and unknown valid item IDs; catalog
changes must not replay first-run rewards. Commit candidates before publishing
live state or success effects. Keep granted balances and grant receipts in one
durable consistency boundary. A focused interface is not a reason to split files.
Failed commits must not leak candidate objects to readers holding old references.

Legacy PersistenceBootstrap still installs its lazy Database factory and its
default path still registers all package definitions. P2-A preserves that behavior;
the explicit example never accesses Database. This is not complete elimination of
global hooks or an automatic switch for existing scenes. Legacy retirement,
TemplateGame aggregate extraction and external consumer cutovers require separate
compatibility plans. No gameplay save migration is implied for BumpHero/ForestBloom.

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

## Horizon 1 BW4 explicit Items/rewards

Opt-in focused presentation, immutable flat reward offers, frozen pending payloads and checked recoverable feature finalization. Existing managers, assets, ItemId values and aggregate Item/IAP APIs remain compatibility paths. Schema 2 applies only to the selected explicit DailyReward/SpinWheel definitions; never compose legacy owners against that same selected root. No game integration or release acceptance is claimed.

See [explicit composition, schema and recovery contract](Documentation~/bw4-explicit-rewards.md). Evidence: `Docs/reports/horizon1-boundaries/service-architecture/bw4-items-rewards/`.

## GameConfig compatibility boundary

`GameConfig` remains a legacy compatibility ScriptableObject in `Hung.Data`.
`DataManager.GetSOData<GameConfig>()` still returns its assigned configuration;
this change does not select a configuration or activate Ads for a product.
The package GameConfig asset retains its identity and values (8, 2, 30, 4), with
its script reference repaired to the existing GameConfig type.

Products own their selected monetization policy. The package asset is not an
automatic fallback and must not silently replace a product's authored values.
GameConfig extraction/API retirement requires explicit caller, serialized asset
and release compatibility treatment. TemplateGame Data integration is outside
this package repair. The legacy DataManager prefab's unresolved config reference
and AdsData asset's stale script reference require separate dispositions.

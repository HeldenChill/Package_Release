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

# Hung Data (L2)

Data framework: SO definitions, save, DataManager. Second assembly: Hung.Common - holds only `ConditionBarrier.cs` (its former `SStats.Stat`/`StatModifier`/`StatModType` duplicate was deleted 2026-07-11, Hung.Base is now sole owner). Kept as a separate assembly deliberately (Ph3 Task 3.3, 2026-07-11): dissolving it into Hung.Data would mean repointing 8 consumer asmdefs for zero functional gain - not worth the churn for one file.

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

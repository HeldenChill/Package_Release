# Changelog

## [0.11.1] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.11.0] - 2026-08-14
### Breaking
- Removed PVM-owned `Runtime/CharacterLegacy` and global `DailyGiftDataSO`/`DailyGiftItem` from Data.
- Added `Hung.Data.LiveOps.IDailyGiftConfig`, `IDailyGiftDay`, and `IDailyGiftReward` as PVM-free DailyGift contracts.
- Re-minted only Data package folder metas that collided with PVM folder GUIDs.

## [0.10.2] - 2026-08-11
- Added the optional ItemCatalog ItemId editor provider in Hung.Data.Editor.

## [0.10.1] - 2026-08-09
- Add: IDataService optional GetSOData<T>(string id = null) with default delegation to the legacy parameterless method.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.10.0] - 2026-07-23
### Added
- Added versioned save envelopes, ordered JSON migrations, beneficial GZip encoding, SHA-256/HMAC integrity protection, separate local key storage, file-backed primary/backup/quarantine mechanics, and legacy PlayerPrefs import.
- Added stable production definitions for `GameData`, DailyGift, Heart, DailyReward, PiggyBank, and SpinWheel.
- Added EditMode fixtures, recovery tests, PlayMode flows, and 10 KB/100 KB/1 MB persistence benchmarks.

### Changed
- Installs the default file-backed persistence service before scene load while retaining legacy PlayerPrefs values as read-only migration evidence.

## [0.9.0] - 2026-07-21
### Changed (BREAKING)
- Removed the legacy `GameplayData.Items` catalog and item migration editor tools after serialized assets moved to `ItemId`.
- Item definitions and generated constants are now the catalog-backed authoring path for item metadata.

## [0.8.0] - 2026-07-14
### Changed (BREAKING)
- `DataManager` no longer declares `[SerializeField] IAPData iapData` or the `typeof(IAPData)` branch in `GetSOData<T>`. `IAPData` is shipped by **com.hung.services.iap**, so referencing it directly made this package uncompilable in any project that does not install IAP - which Horror1Game does not.
- New optional hook `partial void TryGetServiceSOData<T>(ref T result)`, implemented by com.hung.services.iap from its own `Hung.Data.asmref` folder. It is separate from `TryGetGameSOData<T>` because a partial method takes exactly one implementation and the game already owns that one. Serialization is unaffected: same class, same `iapData` field name, so existing DataManager prefab wiring is preserved.
### Why
Wave 2 of the Horror1Game adoption (B4). Same class of defect as the game content removed in 0.7.0, one layer down: a framework package hard-bound to an optional package's type.

## [0.7.0] - 2026-07-14
### Removed (BREAKING)
- `LevelDataSO.cs` - TemplateGame's rice-level ScriptableObject. Zero framework consumers; moved to the game.
### Changed (BREAKING)
- `DataManager` is now `public partial class` and no longer hardcodes any game's ScriptableObjects. Its `typeof` switch keeps only the framework's own (`GameConfig`, `PoolData`, `IAPData`); a game registers its own by declaring another `partial class DataManager` from its `Hung.Data.asmref` folder and implementing the optional hooks `TryGetGameSOData<T>`, `TryGetGameData<T>` and `OnFirstInitData`. Unity serializes by field name within the assembly, so **existing DataManager prefab wiring survives** - no save/prefab migration.
- `GameplayData` is now `public partial class` holding only what the framework reads: `Items`, `MaxHearts`, `HeartCooldownTime`, `ReviveHeartCost`. Its rice-game tunables (toppings, vehicles, surfaces, place types, star curve) moved to the game's partial. Same class, same asset, same field names - `GameplayData.asset` keeps deserializing.
### Fixed
- `GetSOData<T>` had a dead branch returning `iapData` for `typeof(GameData)`; `GameData` is not a `ScriptableObject`, so the `T : ScriptableObject` constraint made it unreachable. Removed.
### Why
Companion to `com.hung.base` 0.11.0 - see that changelog. Moving base's game content out forced this: `LevelDataSO`/`GameplayData`/`DataManager` were the only package-side consumers of the types that left.


## [0.6.0] - 2026-07-11
### Changed
- **Migration (B1 Pass 3 - base):** the 2 files deferred from Pass 2 (`IDataService.cs`, `Locator.Data.cs`, `Runtime/Base/`) renamed `Base`->`Hung.Base`, alongside `com.hung.base`'s own Pass 3 rename.

## [0.5.0] - 2026-07-11
### Changed
- Namespace pass (B1 Pass 2, partial — see notes): `_Game.Managers`→`Hung.Data` (`DataManager`), `Common`→`Hung.Common` (`ConditionBarrier`). `rootNamespace` `_Game.Data`→`Hung.Data` (`Hung.Data.asmdef`), `Common`→`Hung.Common` (`Hung.Common.asmdef`). **Migration:** `using _Game.Managers;`→`using Hung.Data;`; `using Common;`→`using Hung.Common;` wherever `ConditionBarrier` is consumed (found in `Hung.Audio`, `Hung.LiveOps.PiggyBank`, `Hung.LiveOps.SpinWheel`, `Hung.Ads`, `Hung.Analytics`, and 6 TemplateGame UI scripts).
### Notes
- Facts-table census for this pass was stale (same pattern as Pass 1): plan expected `_Game`(11)/`_Game.Data`/`_Game.Character` counts that don't match the live tree. Real census: `_Game.Managers` was 1 file, `Common` was 1 file, `Hung.Data` (2 files) was already compliant.
- **Two families deliberately deferred, not renamed this pass:** `namespace Base` (`IDataService.cs`, `Locator.Data.cs` — physically in this package but namespace-owned by Base, belongs to Pass 3); `Utilities.Core.Data`/`Utilities.Core.Character` (`Runtime/CharacterLegacy/*.cs`, 4 files — namespace-owned by Character, 44 of 48 total consumers live in `Assets/_Game/_Gameplay/_Character`/`_Skill`/`_Harness`, belongs to Pass 4 gameplay per the plan's own squat-follows-namespace precedent, not this data pass).

## [0.4.0] - 2026-07-11
### Removed
- `Hung.Common`'s duplicate `SStats.Stat`/`StatModifier`/`StatModType` (Ph3 Task 3.2, SStats single-owner). `Hung.Base`'s copy (also has `TrackingStat`) is now sole owner. **Migration:** any code that referenced these types via `Hung.Common` specifically (rather than the shared `SStats` namespace) needs a `Hung.Base` reference instead — all known consumers already had one.

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (mechanical move, no code changes).

## [0.2.0] - 2026-07-07
- Add ICharacterStats + ICharacterDefinition contracts (from Horror1Game, canon per canon-decisions.md).

## [0.3.0] - 2026-07-09
- Add `Runtime/LiveOps/{DailyGift,DailyReward,Heart,PiggyBank,SpinWheel}/` — save/SO classes moved in from the 5 `_SubSystem` extractions in Phase 5 (`DailyGiftDataSO`; `DailyRewardDataSO`/`DailyRewardSaveData`; `HeartSave`; `PiggyBankDataSO`/`PiggyBankSaveData`; `SpinWheelDataSO`/`SpinWheelSave`). Each subfolder's manager+UI now lives in its own `com.hung.liveops.*` package; contracts live in `com.hung.base/Runtime/Services/Contracts/<Name>/`.

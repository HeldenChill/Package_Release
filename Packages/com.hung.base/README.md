# Hung Base (L1)

Core runtime: Locator + service contracts (LocatorServices), init flow (InitManager/LoadStart), Stats, GameData, base app glue. Second assembly: Hung.Utilities.Input.

## ItemId Runtime Contract

Item identity is `ItemId`: a serialized, namespaced value such as `base.gold` or `pet_vs_monster.gem`. Runtime save data and item service APIs use `ItemId`; the removed numeric item enum is no longer part of the contract.

`GameData.SaveKey` is `GameData.item-id-v1`. Call `InitData(IEnumerable<ItemId>)` with catalog IDs during startup. Existing unknown saved IDs are preserved; missing catalog IDs are added without deleting saved entries.

Use `Locator.Items.GetPresentation(id)` for icons, display names, and rarity. Do not read metadata from `GameplayData.Items`; that legacy catalog has been removed.

## Persistence Contract

`Hung.Base.Persistence` owns the DI-neutral contracts used by save consumers. Existing synchronous calls remain valid:

```csharp
Database.Save(gameData, GameData.SaveKey);
GameData loaded = Database.Load<GameData>(GameData.SaveKey);
```

`Database` is a compatibility facade. New package internals should receive `IPersistenceService` explicitly and use a registered `SaveDefinition<T>`. Installing `com.hung.data` configures the default service before scene load. A Base-only project must assign `Database.ServiceFactory` and `Database.CompatibilityDefinitionFactory`; it does not fall back to raw PlayerPrefs persistence.

Known debt (carried from extraction, see Docs/audit/canon-decisions.md):
- Hung.Base.asmdef references spine-unity, Unity.TextMeshPro, Unity.InputSystem - vendor refs in L1; spine removal planned Phase 1b.

## Purchase Integrity Contracts

Base owns only vendor-neutral purchase contracts. It does not reference Unity Purchasing, Steamworks, store SDKs, receipts, or product reward data.

Use `PurchaseProductId` as the stable logical identity (`starter-pack`, `gold.pack_1`). `IPurchaseIntegrityService` returns explicit availability, purchase, restore, reconcile, and transaction snapshot results. `IPurchaseGrantHandler` is implemented by the consuming game because reward mutation and entitlement state are game-owned.

DesktopPremium projects that sell the game but do not sell in-game transactions can install `UnsupportedPurchaseIntegrityService` or keep only the legacy `UnsupportedIapService`. Both fail deterministically and never report a fake purchase success.

## Ads Request Contracts

Base owns the vendor-neutral Ads request/result surface. `AdsRequestId`, `AdsShowRequest`, `AdsShowResult`, and `IAdsRequestService` describe request-scoped rewarded and interstitial completion without referencing mediation SDK types.

`AdsShowResult.IsEarnedReward` is true only for rewarded requests that complete with provider reward evidence. `ShouldContinueFlow` allows callers to continue after completed, skipped, unavailable, or unsupported outcomes while treating misconfiguration and overlap as blocking failures.

## Pause Lease Service

`IPauseService` coordinates pause ownership through `PauseLease` values. Releasing an Ads lease releases only that Ads request and never clears active popup, tutorial, gameplay, application, or debug pauses.

`PauseService` writes through `ITimeScale`; production composition can use `UnityTimeScale`, while tests can inject a deterministic scale wrapper. `Locator.Pause` is a compatibility slot during composition migration.

## Time And Reward Integrity Contracts

Base owns the neutral UTC clock and reward claim contracts used by LiveOps packages. `IClock.UtcNow` must be UTC; production code uses `SystemClock`, while tests and composition can inject deterministic clocks.

`RewardDayPolicy` resolves stable `RewardDayKey` values from UTC instants and a reset offset in minutes after midnight UTC. New time-gated rewards must persist full reward-day keys, not day-of-year-only values.

`RewardClaimId.Create(feature, track, cycle, slot, definition, profile)` creates deterministic profile-scoped IDs with FNV-1a 64-bit lowercase hex over canonical UTF-8 parts. Domain callers own stable identity; retryable claims must not use runtime-generated GUIDs.

`IRewardClaimCoordinator` describes the recoverable reward claim protocol. It persists intent, calls an idempotent `IRewardGrantService`, and finalizes feature state, but it is not a physical cross-store transaction. The consuming game must persist item balance and reward receipt together for exactly-once value.

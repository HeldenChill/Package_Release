# Hung Services IAP

Purchase integrity pipeline for MobileF2P games that sell in-game products through Unity Purchasing.

Status: **candidate**. EditMode and PlayMode recovery tests pass in this repo. Real Google Play / Apple App Store sandbox purchases, restore, cancellation/deferred, offline reconnect, and production player-build evidence remain required before stable promotion.

## Boundary

`com.hung.services.iap` owns:

- logical product catalog and store-ID mapping
- durable fail-closed transaction ledger
- transaction coordinator and per-transaction lock
- local Unity receipt validator seam
- Unity Purchasing v5 adapter
- obsolete `IIAPService` compatibility bridge

It does not own:

- reward quantities, item icons, prices shown by custom UI, or economy balance mutation
- game entitlement lists
- authoritative analytics emission
- Steamworks or any DesktopPremium purchase implementation

Vendor-neutral contracts live in `com.hung.base`. Game reward logic implements `IPurchaseGrantHandler`.

## Runtime Pipeline

```text
UI/composition
  -> IPurchaseIntegrityService.PurchaseAsync(logicalProductId)
      -> IPurchaseStoreAdapter observes store transaction
      -> IPurchaseLedger records Observed
      -> IPurchaseValidator validates receipt
      -> IPurchaseLedger records GrantPending
      -> IPurchaseGrantHandler grants value and persists game marker
      -> IPurchaseLedger records Granted
      -> IPurchaseStoreAdapter confirms store order
      -> IPurchaseLedger records Completed
```

Confirmation is intentionally last. A store order is not confirmed until the game grant has succeeded durably.

## Product Catalog

Use `PurchaseProductId` as the stable logical identity. Examples:

```csharp
new PurchaseProductId("starter-pack");
new PurchaseProductId("gold.pack_1");
```

`PurchaseCatalogEntry` maps the logical ID to Google Play, Apple App Store, and editor test IDs. Rewards are not stored in the catalog; the consuming game owns reward policy and versioning.

`LegacyPurchaseProductMap` is explicit. Do not infer logical product IDs from `IAP_ITEM` enum names.

## Ledger

The ledger save key is `purchase-ledger`, schema `1`, using `SaveFailurePolicy.FailClosed`. It stores transaction identity, logical/store IDs, state, receipt fingerprint, minimal validation metadata, timestamps, and error code. It never stores raw receipts.

Crash recovery rules:

- before grant: redelivery retries validation/grant
- grant persisted but ledger still `GrantPending`: game handler returns `AlreadyGranted`
- after `Granted`: retry store confirmation only
- damaged/unrecoverable ledger: no grant, no silent reset

Completed transaction IDs are retained indefinitely in this wave.

## Validation

`UnityLocalPurchaseValidator` wraps a pluggable `IUnityReceiptValidationBackend`. Production Android/iOS composition must provide Unity obfuscation/tangle-backed validation. Missing or invalid validator configuration is blocking; production must not fall back to always-valid validation.

Local validation and local HMAC persistence are tamper evidence, not server authority. High-value games still need server receipt validation and server-authoritative balances.

## Legacy API

`IIAPService` remains source-compatible but obsolete. `LegacyIapServiceAdapter` calls `IPurchaseIntegrityService` and reports success only for durable completed purchases. Unsupported, rejected, cancelled, retryable, deferred, incomplete, missing mapping, or debug bypass paths fail closed.

## DesktopPremium

Windows/Steam DesktopPremium games that only sell the game can omit this package. Base provides `UnsupportedPurchaseIntegrityService` and `UnsupportedIapService` for deterministic unsupported behavior. No Steam transaction work is implemented in this wave.

## Tests

Run focused purchase suites in Unity:

```powershell
# EditMode
Hung.Base.Tests
Hung.IAP.Tests
Hung.IAP.Editor.Tests

# PlayMode
Hung.IAP.PlayModeTests
```

Latest local evidence during this wave:

- EditMode Base + IAP + Editor + Items gate: 99/99 passed
- PlayMode purchase recovery: 7/7 passed

Pre-existing Unity console noise may include old unrelated test errors. Trust fresh test summaries, not stale console history.

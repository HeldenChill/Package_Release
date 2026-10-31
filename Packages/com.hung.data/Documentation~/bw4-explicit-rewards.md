# Explicit BW4 Item/reward composition

This is the opt-in package path. Existing automatic managers, prefabs, `IItemService`, `Locator.Items`, BaseItemIds and IAP aggregate reward APIs remain compatibility paths. This wave does not migrate a game, remove shared GameData, publish packages or certify paid-value/device behavior.

Owner's development-data policy after review: historical DailyReward claim adoption is excluded from this wave. Current projects and TemplateGame may start from fresh/reset development data. Reset related feature, inventory and receipt state consistently before adopting the explicit path; do not mix an old unfinished DailyReward ledger with fresh eligibility. No automatic save deletion is supplied by this repair.

Repair behavior: an unset legacy day key (`0`) initializes to the current reward day without clearing existing progress or spin allowances. Later genuine day rollover still resets eligibility normally. Null BW4 pending/consumed fields are omitted from legacy JSON; populated fields remain persisted in schema-2 state.

## Responsibilities

Product owns ItemId contents, inventory acceptance, lock/spend policy, save roots/key purposes, profile identity and balance/receipt persistence. Base owns focused contracts/defaults. Data provides pure flat offer resolution, fingerprint encoding, state ports and the existing recoverable coordinator. DailyRewardSession and SpinWheelSession own eligibility and feature finalization; their views never grant value independently.

`IItemBalanceReader` Found includes zero. `IItemSpendService` success requires durable commit followed by publication. `IRewardGrantService` commits all balances plus the receipt in one product save, validates every line before mutation, preserves unknown stored rows, and replays identical historical claim/fingerprint without another grant. Product fixtures demonstrate the boundary; no generic wallet implementation is supplied by this wave.

## Flat configuration

```csharp
using System.Collections.Generic;
using Hung.Data.Rewards;

public static class ExampleRewards
{
    public static ResolvedRewardOffer Build()
    {
        return new RewardOfferDefinition {
            key = "starter", revision = "1",
            items = new List<RewardLine> {
                new RewardLine("fixture.coins", 5),
                new RewardLine("fixture.hammer", 1)
            }
        }.Resolve(id => id.Value == "fixture.coins" || id.Value == "fixture.hammer");
    }
}
```

Resolve rejects empty/invalid/default/nonpositive/unknown lines, merges duplicates with checked addition and ordinal-sorts copied immutable lines. Catalog/presentation membership is separate from inventory membership. LegacyRewardTranslation needs an explicit entry for every selected old `(ItemId, quantity)` pair, including direct items; it has no built-in booster interpretation. Pass translated resolved offers or new authored definitions, never silently combine two configuration modes.

The selected wheel index and rate policy remain host/feature configuration. A host can retain the existing SpinWheelDataSO weighted index selection and pass that index into the session. Configuration arrays are copied. Recreate a session explicitly for changed config; pending payloads retain their original contents.

## Delayed composition example

The product supplies one validated persistence graph containing its wallet and exactly selected feature definitions. `createPorts` must be a delayed closure that builds that complete graph, preflights metadata before store construction, and resolves any key provider only inside the selected initializer. Do not call RegisterAll/CreateAll or start legacy automatic owners on the same root. The example receives already typed ports to avoid inventing a second builder or key system.

```csharp
using System;
using Hung.Base;
using Hung.Base.Composition;
using Hung.Base.Persistence;
using Hung.Data.Rewards;
using Hung.LiveOps.DailyReward;

public static class ExampleRewardComposition
{
    public static RewardFeatureBinding<IDailyRewardService, DailyRewardSession> BindDaily(
        Func<(IPersistenceService Persistence,
              SaveDefinition<DailyRewardSaveData> Daily,
              SaveDefinition<RewardIntegrityStateData> Integrity,
              IRewardGrantService Grant)> createPorts,
        IClock clock, RewardDayPolicy days, string profile,
        ResolvedRewardOffer free, ResolvedRewardOffer[] sequence, bool[] ads)
    {
        return new RewardFeatureBinding<IDailyRewardService, DailyRewardSession>(() => {
            var ports = createPorts();
            if (ports.Grant == null || ports.Grant is UnsupportedItemCapabilities)
                throw new InvalidOperationException("Product grant capability not ready.");
            var coordinator = new RewardClaimCoordinator(clock, days, profile, ports.Persistence, ports.Integrity, ports.Grant);
            return new DailyRewardSession(new RewardStateStore<DailyRewardSaveData>(ports.Persistence, ports.Daily),
                coordinator, clock, days, profile, free, sequence, ads, 3600);
        }, session => {
            bool success = session.Initialize(out string code);
            return new FeatureStartResult(success, code);
        });
    }

    public static FeatureHost CreateHost(FeatureRegistration inventory,
        RewardFeatureBinding<IDailyRewardService, DailyRewardSession> daily)
    {
        var host = new FeatureHost();
        host.Register(inventory);
        host.Register(daily.Registration("rewards.daily", true, new[] { inventory.Id }));
        return host;
    }
}
```

Call `host.Start()` only after choosing this host. Bind the view to `daily.Current` only when ProductReady is true. Dispose the old host/session before switching profile or creating its replacement. A concurrent active slot blocks factory execution. Stale Stop removes only its exact instance. Independently selected capability registrations must follow the same preflight/rollback rule; the binding itself owns one service slot, not a hidden multi-slot transaction.

Use `Bw4RewardSaveDefinitions.DailyReward` / `.SpinWheel` for schema 2, and existing `PackageSaveDefinitions.RewardIntegrity` for schema 1. Canonical keys and legacy aliases remain unchanged. The schema-2 migration adds `bw4Pending` and `bw4ConsumedClaimId`; all old pending fields remain readable. It does not invent a payload. Selected definitions use FailClosed. Never run schema-1 legacy owners against a schema-2 selected root. Rollback to old readers needs an explicit compatible package/root plan; do not downgrade saved envelopes.

Migrations clone unknown JSON members, but later typed model round trips do not promise unknown-member preservation. Product extension/converter policy is separate. No product schema/root/alias/protection changes are part of BW4.

## Claim identity and bytes

New `bw4:v1:` fingerprints are lowercase SHA-256 hex over these fields, in order: `bw4:v1`, profile, feature, occurrence, offer key, revision, line count, then each ordinal-sorted ItemId and quantity. Every string is UTF-8 preceded by signed nonnegative Int32 byte length. All Int32 values use little endian. Quantities/counts are Int32. There is no Unicode normalization, trimming or culture conversion. Existing `RewardFingerprint.NormalizeItems` remains unchanged.

Claim IDs preserve established RewardClaimId.Create occurrence rules. A pending free claim retains its originally allocated token; a pending spin retains ordinal/index/day/authorization. Never create another ID because a prior attempt failed or a fingerprint conflicted. Historical coordinator records/receipts retain exact old IDs and fingerprint bytes. Same identity with changed lines is Conflict even if a caller reuses a fingerprint string. Payload-less historical finalized records retain fingerprint-only replay compatibility.

Old pending spins require a trusted retained historical configuration snapshot. `SpinWheelSession.AdoptLegacyPending` verifies the old item/quantity/index/mode/authorization fingerprint and claim ID, then saves the archived expanded payload with the exact old fingerprint. Snapshot contents must come from the original archive, not the current config; the old fingerprint did not encode expanded bundle membership and cannot authenticate that archive. Without archive proof, keep `LEGACY_PENDING_SNAPSHOT_REQUIRED`, preserve evidence and do not register a ready session or auto-clear state.

## Durability and recovery

Pending feature data is saved before preparation is acknowledged. Coordinator intent, product inventory and feature progress are separate durable records. Product receipt is the value idempotency boundary. Feature finalization uses absolute progress/counters and a consumed identity. It retains pending data until coordinator Finalized is durable, then clears pending through another checked save.

On any failure, keep the original identity for retry. A failed feature/coordinator save after a successful inventory commit must not grant value again. Recovery-save failure is not counted as durable recovery. A pending old-day operation is settled before day reset; it cannot consume a new day's allowance. Clock rollback, wrong profile, missing authorization, unsupported grant and invalid snapshots fail closed. General rollback under arbitrary physical faults or concurrent writers is not promised.

Only selected feature initializers read saves or resolve keys. The product chooses separate roots for profile scopes. A coordinator owns one profile's integrity graph; do not share its state file between unrelated profiles or construct it with a grant participant from another profile. Recovery handles all records in that selected profile; feature finalization belongs to its selected session.

## Views

`DailyRewardOfferView` and `SpinRewardOfferView` are new opt-in components. Their `contents`, `status` and button fields can be wired in later product integration; this wave uses transient package test objects. ItemCatalogPresentationProvider supplies detached presentation. Every grant line is shown by display name and amount; missing presentation disables preparation. A summary icon is optional and cannot replace expanded contents.

Daily view reports Claimed only after actual success. Spin view emits Prepared only after pending save succeeds; host may animate and then call Complete. Completion failure keeps the pending ID for retry. Effects run only for a newly successful grant, not replay; presenter unavailability never changes committed value. A headless feature does not require a presenter. Existing popup/prefab behavior is not certified by these new component tests.

## Remaining gates

Legacy Item/IAP/Ads/DailyGift/PiggyBank callers, existing automatic managers, product schemas/assets, game integration, actual player/store/provider confidence, immutable release and full Horizon 1 closure remain separate. Evidence belongs under `Docs/reports/horizon1-boundaries/service-architecture/bw4-items-rewards/`. Report actual Unity XML, builds, source hashes and validator identity deltas; plan-author .NET checks are not Unity or consumer acceptance.

# Hung LiveOps SpinWheel

## Purpose

SpinWheel LiveOps feature: spin manager and popup UI. Contracts live in `com.hung.base`, save data models in `com.hung.data`.

## Layer + dependencies

- Layer: `4`
- Dependencies: `com.hung.designpattern` 0.4.2, `com.hung.utilities` 0.2.1, `com.hung.base` 0.18.0, `com.hung.data` 0.10.0, `com.hung.ui` 0.5.1

## Prerequisites

- DOTween (Asset Store) - wheel spin animation
- TextMeshPro (`Unity.TextMeshPro`, Unity 6 built-in)

## Quick start

Drop in `SpinWheelManager` + `SpinWheelPopup` (see `Samples~/`), configure the reward table, and call `ConfigureTimeRewardIntegrity` from composition to inject a UTC clock, reward-day policy, stable profile scope, reward data, and reward claim coordinator.

## Public API index

- `SpinWheelManager` - spin logic, prepared reward intent, daily availability, and claim completion
- `SpinWheelPopup` - popup UI presenting the wheel and spin trigger

## Known limitations / sharp edges

- Claims fail closed until a reward claim coordinator is configured.
- Config is game content (ScriptableObject instances); the package ships scripts only.
- EditMode tests cover UTC reset, rollback freeze, prepared completion, and ad authorization.

## Samples

`Samples~/` - install prefab + wiring guide (not yet exposed via the `samples` array - Stage A Ph4 Task 4.3).

## Horizon 1 BW4 explicit Items/rewards

Opt-in focused presentation, immutable flat reward offers, frozen pending payloads and checked recoverable feature finalization. Existing managers, assets, ItemId values and aggregate Item/IAP APIs remain compatibility paths. Schema 2 applies only to the selected explicit DailyReward/SpinWheel definitions; never compose legacy owners against that same selected root. No game integration or release acceptance is claimed.

See [explicit composition, schema and recovery contract](../com.hung.data/Documentation~/bw4-explicit-rewards.md). Evidence: `Docs/reports/horizon1-boundaries/service-architecture/bw4-items-rewards/`.

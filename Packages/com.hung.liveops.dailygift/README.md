# Hung LiveOps DailyGift

## Purpose

Daily gift LiveOps feature: login-streak manager and popup UI. Service contracts live in `com.hung.base`; persistence and neutral DailyGift configuration contracts live in `com.hung.data`.

## Layer + dependencies

- Layer: `4`
- Dependencies: `com.hung.designpattern` 0.4.3, `com.hung.base` 0.19.2, `com.hung.data` 0.11.0, `com.hung.ui` 0.5.3

## Prerequisites

- TextMeshPro (`Unity.TextMeshPro`, Unity 6 built-in)

## Quick start

Drop in the `DailyGiftManager` + `DailyGiftPopup` install prefab (see `Samples~/`), wire it to your save/UI systems, and assign each config field a `ScriptableObject` implementing `Hung.Data.LiveOps.IDailyGiftConfig`. PVM's existing `DailyGiftDataSO` is intended adapter. Composition can call `ConfigureTimeRewardIntegrity` to inject a UTC clock, reward-day policy, stable profile scope, and reward claim coordinator.

## Public API index

- `DailyGiftManager` — login-streak tracking + claim logic
- `DailyGiftPopup` — popup UI presenting the current day's gift
- `DailyGiftUIItem` — per-day item widget inside the popup
- `IDailyGiftConfig` (from `com.hung.data`) — required product-owned configuration contract

## Known limitations / sharp edges

- Streak tracking is device-local time (cheatable via clock manipulation) — accepted for single-player use.
- Config is game content (ScriptableObject instances); the package ships scripts only.
- Raises an EventBus event on claim, consumed by game-side UI (e.g. HomeCanvas) — no direct UI coupling.
- EditMode tests cover UTC day progression, rollback freeze, and coordinator-backed claim finalization.

## Samples

`Samples~/` — install prefab + wiring guide (not yet exposed via the `samples` array — Stage A Ph4 Task 4.3).

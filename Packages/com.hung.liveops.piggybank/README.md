# Hung LiveOps PiggyBank

## Purpose

PiggyBank LiveOps feature: coin-accumulation manager and popup UI with a fly-to-icon claim animation. Contracts live in `com.hung.base`, save data models in `com.hung.data`.

## Layer + dependencies

- Layer: `4`
- Dependencies: `com.hung.designpattern` 0.2.0, `com.hung.utilities` 0.1.0, `com.hung.base` 0.7.0, `com.hung.data` 0.3.0, `com.hung.ui` 0.1.0

## Prerequisites

- TextMeshPro (`Unity.TextMeshPro`, Unity 6 built-in)

## Quick start

Drop in `PiggyBankManager` + `PiggyBankPopup` (see `Samples~/`); on claim, the manager raises a `PiggyBankRewardClaimedEvent{goldAmount}` on the EventBus — subscribe to it game-side to play the fly-to-icon reward animation instead of coupling directly to HomeCanvas.

## Public API index

- `PiggyBankManager` — coin-accumulation + claim logic, raises `PiggyBankRewardClaimedEvent`
- `PiggyBankPopup` — popup UI presenting accumulated coins + claim button

## Known limitations / sharp edges

- Config is game content (ScriptableObject instances); package ships scripts only.
- Claim flow is decoupled from game UI via EventBus (`PiggyBankRewardClaimedEvent{goldAmount}`) — do not re-couple to a concrete canvas.
- No automated tests exist yet for this package.

## Samples

`Samples~/` — install prefab + wiring guide (not yet exposed via the `samples` array — Stage A Ph4 Task 4.3).

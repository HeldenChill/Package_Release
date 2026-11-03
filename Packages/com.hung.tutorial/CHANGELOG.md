# Changelog

## [0.3.14] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.29.0 (init-flow seams). No code change in this package.

## [0.3.13] - 2026-11-02

- Patch release: re-pin dependencies to com.hung.designpattern 0.5.1 (pool statics reset on SubsystemRegistration). No code change in this package.

## [0.3.12] - 2026-10-31

- Dependency alignment: com.hung.base 0.27.0 -> 0.28.0; com.hung.data 0.13.0 -> 0.14.0; com.hung.ui 0.8.1 -> 0.9.0

## [0.3.11] - Unreleased

- Migrate affected service ownership to read-only Locator/ServiceSlots registration and exact-instance cleanup. Base adds explicit Ads/IAP Unsupported defaults and removes product gameplay contracts; see Base migration notes. Unreleased source; consumers are not automatically upgraded.
- Update declared dependency versions for the tested P2-C cohort; this does not activate optional product features.

## [0.3.10] - 2026-09-28
- Dependency alignment: com.hung.ui 0.7.1 -> 0.7.2.

## [0.3.9] - 2026-09-28
- Dependency alignment: com.hung.ui 0.7.0 -> 0.7.1.

## [0.3.8] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6; com.hung.ui 0.6.0 -> 0.7.0.

## [0.3.7] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.3.5] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.3.4] - 2026-08-11
- Dependency-only patch: align Base 0.19.2, Data 0.10.2, and UI 0.5.3 for the F4-IE editor prerequisite.

## [0.3.3] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.ui 0.5.1 -> 0.5.2; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.3.2] - 2026-07-21
- Migrated tutorial reward configuration to canonical `ItemId`; aligned dependency versions with the ItemId release.

## [0.3.1] - 2026-07-15
- **Fix: `TutorialStateMachine.PrepareTutorial` null-refed on any level whose `LoadingDatas` was never authored.** The guard checked `_tutorialData == null` and then dereferenced `_tutorialData.LoadingDatas.ContainsKey(step)` — but `LoadingDatas` is an optional `Dictionary` that Odin leaves **null** when the author never fills that section in. Now `_tutorialData?.LoadingDatas == null`.
- **Fix (same shape): `Tick` dereferenced a possibly-null `ConditionActions`.** That `List` has no initializer either, so a level entry authored without steps null-refed on the first tick. Now guarded.
- Found by TemplateGame's new `TutorialHarness` (B4 Wave 3) — the first thing to ever drive the tutorial state machine in this repo. Pre-existing; adoption did not cause it.

## [0.3.0] - 2026-07-15
- **`TutorialUIController.Acquire` now calls `Locator.UI.GetUIAsync<T>` instead of `GetUI<T>`** for all three tutorial canvases (`TutorialActionsCanvas`, `CallOutCanvas`, `ShowContentPopup`). Requires `com.hung.base` 0.13.0 + `com.hung.ui` 0.5.0.
- **Why:** Horror1Game (B4 Wave 3) loads these three canvases from Addressables. The default `GetUIAsync` implementation is synchronous and invokes immediately, so this is a no-op for TemplateGame — but the canvas fields are now assigned in a callback, so **do not assume they are non-null on the line after `Acquire()` returns** in a game that overrides the loader.

## [0.2.0] - 2026-07-11
- **Migration (B1 Pass 6):** `Tutorial`->`Hung.Tutorial` (6 files), `Tutorial.EditorTools`->`Hung.Tutorial.EditorTools` (2 files). `Base.UI`/bare `UI` files (`CallOutCanvas`/`ShowContentPopup`/`TutorialActionsCanvas`) merged into `Hung.UI` alongside com.hung.ui's own Pass 6 rename. No external consumers of the Tutorial namespace (only `Locator.Tutorial` property access). `rootNamespace` set on both asmdefs (was empty).

## [0.1.0] - 2026-07-07
- Created from Horror1Game's 16-file tutorial graph version (TutorialGraphSO, StateMachine, Registry, editor window) — canon per wave 1 decision (H1 tutorial variant supersedes the baseline 8-file version). GUIDs preserved via meta copy. API break: old `PrepareTutorial(level)→Task` + enum keys (`CUSTOM_CONDITION`/`CUSTOM_ACTION`) replaced by `Run(level)` + string keys, adds `Init/Stop/CurrentStep/IsRunning`. The 4 broken consumers (HomeCanvas, LoadingCanvas, LoseCanvas, WinCanvas) migrated to the new API following H1's same-named canvases as reference.
- `ITutorialService`, `TutorialKeys`, `ENUM.TUTORIAL.cs` (HAND_ACTION/TUTORIAL_LOADING/TUTORIAL_ACTION/POSITION_TYPE), `Locator.Tutorial` moved to `com.hung.base/Runtime/Services/Contracts/Tutorial/` (same-package pattern as UI contracts — Base needs these types visible with zero upward dependency).
- No separate Editor asmdef — Editor/ files compile into `Hung.Tutorial` via `#if UNITY_EDITOR` guard, matching H1's own convention.
- Old `Assets/_Game/_Tutorial` code deleted (asmrefs, scripts, meta). Prefabs (`TutorialManager`, `CallOutCanvas`, `ShowContentPopup`, `TutorialActionsCanvas`) and non-code assets (Data/TutorialData.asset, Base/Tutorial.json, Video/*) left in place deliberately — they now show Missing Script and need manual component re-wiring by the user in Unity (Phase 3 Wave 2b Task 6 runtime checklist).
- 0 compile errors: Hung.Base, Hung.Tutorial, Assembly-CSharp.

# Hung Character

## Purpose

Character core framework: a modular `Character<TDef,TStats>` composed of Logic / Physic / WorldInterface / Brain systems. Canon source: Horror1Game — the package is a refresh of that game's live core, and Horror1Game is its first consumer.

Ships with **BrainGraph**, a self-contained node-graph AI *and its editor*. It needs no external graph package — see Quick start.

## Layer + dependencies

- Layer: `3`
- Dependencies: `com.hung.designpattern` 0.4.1, `com.hung.utilities` 0.2.0, `com.hung.base` 0.14.0, `com.hung.data` 0.8.0, `com.unity.inputsystem` 1.18.0
- Assemblies: `Hung.Character` (runtime), `Hung.Character.Editor` (Editor-only, BrainGraph authoring)

## Prerequisites

- DOTween (Asset Store, `DOTween.Modules` asmdef)
- Unity Input System (`com.unity.inputsystem` 1.18.0, declared dependency)

**`com.unity.behavior` is not required.** If you read that anywhere, it is stale — BrainGraph does not use it (see Design notes).

## Quick start

1. Derive a concrete `CharacterDefinition` / `CharacterStats` pair.
2. Attach a `Character<TDef,TStats>` subclass to your GameObject and compose the modules it needs — Logic, Physic, WorldInterface, Brain, Visual are each a separate component on the same object.
3. **Player-controlled?** Add `PlayerInput` and assign your own `.inputactions` asset to its `actions` field. Defaults expect a `"Movement"` map with `Move` / `Look` / `Jump` / `Crouch` / `Run` actions; override the serialized names to match your asset. `PlayerInput` does **not** enable action maps — your game owns enable/disable (Horror does it through `Locator.Input.SetInput`).
4. **AI-controlled?** Add `BrainGraphModule` and assign a `BrainGraphAsset`. Author graphs via **Tools → Character → Create Default Enemy BrainGraph**, or the BrainGraph editor window.

Spawning: `Awake` initializes from the serialized definition. To inject one instead, call `OnInit(definition)` before the object activates — it is guarded, so the later `Awake` is a no-op.

## Public API index

- `Character.cs` — the modular `Character<TDef,TStats>` component root; `OnInit(TDef)` init seam
- `CharacterDefinition` / `CharacterStats` — per-character static/runtime data contracts
- `LogicSystem` / `LogicModule` / `LogicParameter` / `LogicData` / `LogicEvent` — the module composition pipeline
- `BaseLogicState` / `Logic.States` — state-machine building blocks for character logic
- `BrainModule` / `BrainData` / `BrainParameter` — navigation/AI hookup point
- `PlayerInput` — asset-driven input brain module (`InputActionAsset` + serialized map/action names)
- `BrainGraphAsset` / `BrainGraphModule` / `BrainGraphRunner` — the node-graph AI
- `BaseSensor` (`WorldInterface/`) — perception sensor base for ground/wall/edge detection
- `RegenStatModule` — stat regeneration over time
- `Trigger.cs` — collider-trigger event relay

## Design notes

**BrainGraph needs no external graph package.** It is a hand-rolled node graph written as a *replacement* for Unity Behavior Graph, not a consumer of it — it references `Unity.Behavior` in zero files. Package 0.1.0 exiled it to `Samples~` on the mistaken belief that it required `com.unity.behavior`; 0.2.0 restores it to core. There is no `HUNG_CHARACTER_BEHAVIOR` define and no gated assembly, because there is nothing to gate.

**Two governing laws** (from `character-core-glue-split.md`), worth respecting when extending:

1. **Core holds zero game-specific types.** Your game's `Enemy1AI`, `PlayerDefinition`, `PlayerStats` and similar belong in your own glue assembly, not here. Horror keeps them in `Hung.Character.Game`.
2. **BrainGraph nodes read `BrainParameter` and write `BrainData` only.** That constraint is what keeps graphs reusable across games.

**Extension seams:** override the `Create*System` / `Create*Data` factory virtuals on `Character<TDef,TStats>` to swap in your own system or data types; add a glue assembly referencing `Hung.Character` for game-specific modules (see Horror's `Hung.Character.Game`).

**Input contract:** `Locator.Input` / `IInputService` are canon in `com.hung.base` ≥ 0.14.0. `IInputService` is `partial`, so your game can extend it with its own surface (e.g. generated action accessors) from a `Hung.Base` asmref folder.

## Known limitations / sharp edges

- Version is deliberately **0.x**: one consumer, and the promotion criteria are not met yet.
- **`file:` packages are writable from the consumer** — a consuming project's Unity run can mutate this package's source in place. Check `git status` here after editor sessions in the consuming game.
- `rootNamespace` is empty and types live under `Gameplay.Character.*`, **not** `Hung.Character` — that is the assembly name only. Character was excluded from the framework-wide namespace rename; it is still pending.
- `Trigger.cs` is a third copy of that type in the repo (alongside `com.hung.base`'s `Base.Trigger` and `com.hung.tutorial`'s global-namespace `Trigger`). No collision today; unification deferred.
- No automated tests exist yet for this package.

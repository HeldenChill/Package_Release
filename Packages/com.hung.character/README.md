# Hung Character

## Purpose

Character core framework: a modular `Character<TDef,TStats>` composed of Logic / Physic / WorldInterface / Brain systems. Canon source: Horror1Game — the package is a refresh of that game's live core, and Horror1Game is its first consumer.

AI is optional: install `com.hung.ai` and the `Hung.Character.AI` bridge assembly compiles (see "Hung.AI bridge" below). The old built-in BrainGraph was removed in 0.3.0.

## Layer + dependencies

- Layer: `3`
- Dependencies: `com.hung.designpattern` 0.4.1, `com.hung.utilities` 0.2.0, `com.hung.base` 0.14.0, `com.hung.data` 0.8.0, `com.unity.inputsystem` 1.18.0
- Assemblies: `Hung.Character` (runtime), `Hung.Character.AI` (optional, only with `com.hung.ai`)

## Prerequisites

- DOTween (Asset Store, `DOTween.Modules` asmdef)
- Unity Input System (`com.unity.inputsystem` 1.18.0, declared dependency)

**`com.unity.behavior` is not required.**

## Quick start

1. Derive a concrete `CharacterDefinition` / `CharacterStats` pair.
2. Attach a `Character<TDef,TStats>` subclass to your GameObject and compose the modules it needs — Logic, Physic, WorldInterface, Brain, Visual are each a separate component on the same object.
3. **Player-controlled?** Add `PlayerInput` and assign your own `.inputactions` asset to its `actions` field. Defaults expect a `"Movement"` map with `Move` / `Look` / `Jump` / `Crouch` / `Run` actions; override the serialized names to match your asset. `PlayerInput` does **not** enable action maps — your game owns enable/disable (Horror does it through `Locator.Input.SetInput`).
4. **AI-controlled?** Install `com.hung.ai`, then add `AIAgent` + `AIBrainModule` (see "Hung.AI bridge").

Spawning: `Awake` initializes from the serialized definition. To inject one instead, call `OnInit(definition)` before the object activates — it is guarded, so the later `Awake` is a no-op.

## Public API index

- `Character.cs` — the modular `Character<TDef,TStats>` component root; `OnInit(TDef)` init seam
- `CharacterDefinition` / `CharacterStats` — per-character static/runtime data contracts
- `LogicSystem` / `LogicModule` / `LogicParameter` / `LogicData` / `LogicEvent` — the module composition pipeline
- `BaseLogicState` / `Logic.States` — state-machine building blocks for character logic
- `BrainModule` / `BrainData` / `BrainParameter` — navigation/AI hookup point
- `PlayerInput` — asset-driven input brain module (`InputActionAsset` + serialized map/action names)
- `AIBrainModule` / `CharacterMover` / `BrainDataMap` / perception nodes — optional Hung.AI bridge (`Runtime/AI/`)
- `BaseSensor` (`WorldInterface/`) — perception sensor base for ground/wall/edge detection
- `RegenStatModule` — stat regeneration over time
- `Trigger.cs` — collider-trigger event relay

## Design notes

**Two governing laws** (from `character-core-glue-split.md`), worth respecting when extending:

1. **Core holds zero game-specific types.** Your game's `Enemy1AI`, `PlayerDefinition`, `PlayerStats` and similar belong in your own glue assembly, not here. Horror keeps them in `Hung.Character.Game`.
2. **AI nodes read `BrainParameter` and write `BrainData` only** (through `CharacterMover` and `BrainDataMap`). That constraint is what keeps graphs reusable across games.

**Extension seams:** override the `Create*System` / `Create*Data` factory virtuals on `Character<TDef,TStats>` to swap in your own system or data types; add a glue assembly referencing `Hung.Character` for game-specific modules (see Horror's `Hung.Character.Game`).

**Input contract:** `Locator.Input` / `IInputService` are canon in `com.hung.base` ≥ 0.14.0. `IInputService` is `partial`, so your game can extend it with its own surface (e.g. generated action accessors) from a `Hung.Base` asmref folder.

## Known limitations / sharp edges

- Version is deliberately **0.x**: one consumer, and the promotion criteria are not met yet.
- **`file:` packages are writable from the consumer** — a consuming project's Unity run can mutate this package's source in place. Check `git status` here after editor sessions in the consuming game.
- `rootNamespace` is empty and types live under `Gameplay.Character.*`, **not** `Hung.Character` — that is the assembly name only. Character was excluded from the framework-wide namespace rename; it is still pending.
- `Trigger.cs` is a third copy of that type in the repo (alongside `com.hung.base`'s `Base.Trigger` and `com.hung.tutorial`'s global-namespace `Trigger`). No collision today; unification deferred.
- No automated tests exist yet for this package.

## Hung.AI bridge (optional)

`Hung.Character.AI` compiles only when the `com.hung.ai` package (0.1.0 or newer) is installed. It lets a character be driven by a Hung.AI behavior-tree graph.

Setup on a character:

1. Add `AIAgent` (from `com.hung.ai`) and assign the graph. Its Auto Tick is turned off at runtime; the brain ticks with the character.
2. Add `AIBrainModule` and assign it to the character's Navigation (Brain) module slot.
3. Optional: a `NavMeshAgent` on the same object gives NavMesh paths. Without it the mover heads straight at the target. The NavMeshAgent never moves the body; Logic/Physic do, from `BrainData.MoveDirection`.
4. Optional: in `AIBrainModule > Brain Data Map`, bind blackboard keys to `BrainData` fields that game code reads (for example a bool key to `IsSeePosition`).

| Node (Character/Perception) | Does |
|---|---|
| Sense World | Reads sight and hearing sensors every tick; always Running. Put it in a Parallel beside the decision branch |
| See Target | True while a target is seen or remembered; writes target Transform and last seen position |
| Hear Sound | True while a sound is heard or remembered; writes sound position |
| Track Target | Writes the seen target's live position every tick |
| Lose Target | Forgets the target (and sound), clears keys |
| Look Around | Stops and turns on the spot for a while |

Movement uses the core `Move To`, `Patrol` and `Stop Move` nodes through `CharacterMover`.

The bridge skips blackboard mapping while navigation is paused. Successful Move To and non-looping Patrol nodes use the optional `IAIMovementCompletion` interface to stop movement while retaining `HasArrivedAtDestination` and the final `RemainingDistance` for character logic. Explicit Stop, pause, reset, or aborted movement clears that completion state. Existing movers implementing only `IAIMover` continue to receive `Stop()`.

The NavMesh mover waits for pending path calculation, then rejects partial or invalid paths and clears movement intent. A failed path is reported to the graph rather than retried before its result can be read.

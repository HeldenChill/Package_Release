# Changelog

## [0.2.7] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.2.6] - 2026-09-20
- Performance: eliminated `CharacterBase.Update()` GC allocations (1,004 B -> 0 B). Cached BrainGraph node lookups (`nodeMap`) and pre-sorted outgoing edge lists (`GetOutgoingCached`).
- Performance: replaced dynamic string interpolation in `PerceptionNode` and `ConditionNode` with static constants under `DebugRuntime`.
- Dependency alignment: com.hung.base 0.21.1, com.hung.data 0.12.4.

## [0.2.4] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.2.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 and Data 0.10.2 for the F4-IE editor prerequisite.

## [0.2.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.2.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no character API or runtime behavior changed.

## [0.2.0] - 2026-07-17

**Un-parked.** Runtime refreshed wholesale from Horror1Game's live core (Horror's `.meta` GUIDs win), which is now
the canon character architecture. Horror1Game is the first real consumer (B4 Wave 6).

### Two 0.1.0 decisions reversed — both rested on false premises

- **BrainGraph is back in the package core, out of `Samples~`.** 0.1.0 exiled it on the belief that it needed
  `com.unity.behavior`, a dependency Package_Repo lacks. It does not: BrainGraph references `Unity.Behavior` in
  **zero files** — it is a self-contained node-graph AI written as a *replacement* for Unity Behavior Graph
  (`BrainGraphModule.cs`: "runs a local Character BrainGraph without Unity Behavior Graph runtime dependency").
  The subsystem had been dropped because the *extraction environment* lacked a dependency it never used.
  `Samples~/BrainGraph` (69 files) deleted; the `samples` entry is gone from `package.json`. No
  `HUNG_CHARACTER_BEHAVIOR` define and no gated assembly are needed — nothing to gate.
- **`Enemy1AI.cs` was the only real `Unity.Behavior` consumer**, and it is game glue, not core. It now lives in
  Horror's `Hung.Character.Game`. Core's asmdef consequently drops `Unity.Behavior` +
  `Unity.Behavior.SerializableGUID`; the package core is now Behavior-free **and** Odin-free.

### Added

- `Base/BrainSystem/BrainGraph/**` (24 runtime files) — the node-graph AI, as ordinary core content.
- `Editor/BrainGraph/**` (6 files) + `Hung.Character.Editor.asmdef` — the graph authoring window, menus and node
  codegen. 0.1.0 shipped the graph system with no way to author graphs; a real `Editor` asmdef
  (`includePlatforms: ["Editor"]`) now excludes it from player builds structurally, superseding 0.1.0's
  `#if UNITY_EDITOR` wrapping (that guard existed only because the files sat in `Runtime/Editor/` with no editor
  asmdef). Runtime has zero `UnityEditor` usage — verified.
- `PlayerInput` is now core, not glue. It takes an `InputActionAsset` + serialized map/action names instead of a
  game-generated `PlayerInputActions` wrapper, so it is reusable, and it unsubscribes on destroy (0.1.0's excuse for
  excluding it — "depends on H1's game-specific generated wrapper" — no longer holds).
- `Character<TDef,TStats>.OnInit(TDef definition = null)` — an init seam letting a spawner inject a definition before
  activation; `Awake` bridges to it, guarded so it runs at most once.

### Changed

- `BuildDefaultHorrorEnemyGraph()` → `BuildDefaultEnemyGraph()` (+ menu labels) — the method builds a generic
  see/hear/idle awareness graph from framework node kinds; it carried one game's name for no reason.
- `com.hung.base` dependency 0.13.0 → 0.14.0: the input contract (`partial IInputService` + `Locator.Input`) is now
  canon in base.
- Asmdef GUID aligned to Horror's `e5e264677b498b84bbbe2acc8605130b` (old `bf02a41f…` had zero referencers), so
  adoption needs no asmdef reference edits.

### Harvest audit — nothing ported (all 5 candidates rejected)

Wave 6 planned to harvest 5 types from the `Utilities.Core` reference copy. The audit rejected every one:

| Candidate | Verdict |
|---|---|
| `IDamageable` | **Already canon** in `com.hung.combat` (`Runtime/Core/Combat/IDamageable.cs`) — porting would duplicate the type across packages |
| `IProjectile` | **Already canon** in `com.hung.base` (`Runtime/Gameplay/CharacterContracts/`) |
| `ELEMENTAL` | **Already canon** in `com.hung.base` (`Runtime/Gameplay/CharacterContracts/`) |
| `CHARACTER_TYPE` | **Already canon** in `com.hung.base` (`Runtime/Gameplay/CharacterContracts/`) |
| `Lock<T>` | No collision, but **unused** — no consumer in this package, and the canon donor (Horror's core) does not have it. Adding it would be speculative |

The four contract types were promoted to their proper packages in earlier waves; the plan's harvest list was written
before that. Nothing to do.

### Known

- Horror still carries four orphaned Unity.Behavior graph assets (`EnemyHear`/`EnemySeen`/`EnemySense`/
  `SenseBlackBoard`) whose `*Action` types were superseded by BrainGraph's `*Node` files and exist in no assembly.
  They are Horror-side glue and do **not** ship in this package.
- `Trigger.cs` third-copy debt from 0.1.0 is unchanged.

## [0.1.0] - 2026-07-07
- Extracted wholesale from Horror1Game's BrainGraph character core (canon per `Docs/audit/canon-decisions.md`): `Character<TDef,TStats>`, `ICharacter`, Logic/Physic/WorldInterface/Navigation/BrainSystem module families, `BrainGraphAsset`/`BrainGraphRunner`, `CharacterDefinition`/`CharacterStats`.
- Glue excluded (stays in each game's Assets): `Character/Player/{PlayerDefinition,PlayerStats}.cs`, `Base/Navigation/Module/Enemy1AI.cs`, `Base/Navigation/Module/PlayerInput.cs` (depends on H1's game-specific generated `PlayerInputActions`; each game supplies its own input brain-module via `Locator.Input`), `Player.prefab`, `Enemy1.prefab`, `Docs/`.
- StateMachine API fork resolved by union-merge into `com.hung.designpattern` 0.2.0 (user-signed; supersedes an initial verbatim-copy approach): `Logic/*` files now `using DesignPattern;`. H1's decorator-state API and H1-only STATE members were merged upstream — see designpattern CHANGELOG. Note H1 value drift: `NONE` is -1 (H1 had 0), `WALK=11`/`JUMP=12`/`IN_AIR=13` etc. renumbered, `STUN=9` (H1 had 104) — runtime-only enum, but check on H1 adoption if any STATE value was serialized.
- `BrainModule.Reset()` renamed to `ResetModule()` (user-signed fork — zero callers/overrides in H1; Unity's editor-magic `private void Reset()` on `BrainGraphModule` is untouched).
- `Runtime/Editor/BrainGraph/*.cs` (6 files) wrapped in `#if UNITY_EDITOR` — fixes a latent H1 player-build break (these files had no guards). No separate editor asmdef (package convention); H1's `Hung.Character.Editor.asmdef` was not copied.
- Debt: `Trigger.cs` is a third copy in the repo (`namespace Utilities`, alongside com.hung.base's `Base.Trigger` and com.hung.tutorial's global-ns `Trigger`) — no collision today, unification deferred.

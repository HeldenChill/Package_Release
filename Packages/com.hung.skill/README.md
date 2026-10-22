# Hung Skill Framework

## Purpose

Game-agnostic skill framework: `BaseSkill` lifecycle, `SkillData` per-level configuration, projectile bases (bullet/throw/explosion), collider controller, and VFX controller. Character-coupled glue (`CharacterSkill`, weapons, concrete skills like `SlashSkill`/`FireBallSkill`) intentionally stays game-side in `Hung.Skill.Game`, not this package.

## Layer + dependencies

- Layer: `3`
- Dependencies: `com.hung.designpattern` 0.2.0, `com.hung.utilities` 0.1.0, `com.hung.base` 0.7.0, `com.hung.combat` 0.2.0

## Prerequisites

- Sirenix Odin Inspector — `BaseSkill` uses `[Button]` attributes (attribute-only usage, declared in asmdef)

## Quick start

Subclass `BaseSkill`, populate a `SkillData` asset (one entry per per-level list — see sharp edge below), and drive the lifecycle via `SkillActivation()` from your input/AI layer.

## Public API index

- `BaseSkill` — skill lifecycle base (init, activation, execution)
- `SkillData` — per-level skill configuration ScriptableObject
- `BaseProjectile` / `BaseBullet` / `BaseThrow` / `ExplosionThrow` / `ViroBullet` — projectile base classes
- `ColliderController` — melee/area collider driver
- `EffectVFXController` — VFX playback tied to skill effects

## Known limitations / sharp edges

- **Trigger `SkillActivation()`, NOT `SkillExecute()`** — `SkillExecute()` latches `isExecute = true`, which makes `FixedUpdate` auto-recast the skill every physics frame. This was confirmed the hard way building the TemplateGame damage harness.
- `SkillData` per-level lists (Damages, CoolDowns, etc.) **must be non-empty** — an empty list is a runtime index crash, not a graceful no-op.
- **Don't re-trigger `SkillActivation()` while a `SetActive(true)`-driven VFX GameObject is still active from the previous cast** — `SetActive(true)` on an already-active GameObject doesn't fire `OnEnable`, so a still-playing `ParticleSystem` won't restart even though the underlying `STimer` action sequence itself re-arms correctly every call. Not a framework bug (confirmed via `SkillCastHarness`, see `Docs/validation/harness-log.md`) — wait for the previous cast's `ExistTime` window, or have the VFX-toggle action call `ParticleSystem.Play(true)` explicitly instead of relying on `SetActive`+`OnEnable`.
- No automated tests exist yet for this package.

## Runtime verification
- `SkillActivation()` no-NRE smoke test for the 3 game-side migrated skills (`NormalSlashSkill`/`SlashSkill`/`DefendOrbSkill`): runtime-verified via `SkillCastHarness` (`Assets/_Game/_Harness/SkillCastHarness/`). See `Docs/validation/harness-log.md`.

## Samples

None.

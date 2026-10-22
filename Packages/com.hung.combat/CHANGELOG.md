# Changelog

## [0.4.7] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6.

## [0.4.6] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.4.4] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.4.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 and Data 0.10.2 for the F4-IE editor prerequisite.

## [0.4.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.4.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no combat API or runtime behavior changed.

## [0.4.0] - 2026-07-11
### Changed
- **Migration (B1 Pass 4 - gameplay):** `Base.Combat`->`Hung.Combat` (18 files), `Base.StatusEffects`->`Hung.StatusEffect` (12 files). This package's own squat, deferred by Pass 3. rootNamespace set on all 3 asmdefs.
- Game-side consumers migrated in-pass: `TakeDamageModule.cs`, `NormalSlashSkill.cs`, `OrbisSkill.cs`, `SlashSkill.cs` (`using Base.Combat;`->`using Hung.Combat;`).
- **PvM warning:** combat/statuseffect canon is co-developed with PvM - this Migration map must be applied to PvM's copies when PvM next syncs; recorded in ADOPTION.md.
### Fixed
- `DamageHit.cs` shadow bug: unqualified `StatusEffect` (the global-namespace class defined in `Runtime/Core/StatusEffect/StatusEffect.cs`) resolved to the sibling `Hung.StatusEffect` namespace instead (enclosing `Hung` scope reachability, same shadow class as Pass 1/3) - fixed with `global::StatusEffect`.
- **Census gap found by the compile gate, not the plan:** `AOEDamage.cs` (physically in this package's `Combat/` folder) was still declared `namespace _Game`, not `Base.Combat` - missed by the namespace grep census since it doesn't match the `Base.*` pattern. Renamed to `Hung.Combat` in this pass; its `_Game`-side implicit consumers (`ExplosionThrow.cs`, `KnockBackAllSkill.cs`, `PestilentiaSolumSkill.cs`) needed an explicit `using Hung.Combat;` added since they no longer share a namespace with it.

## [0.3.0] - 2026-07-11
### Changed
- **Migration (B1 Pass 3 - base, dependency):** `com.hung.base` renamed `Base`->`Hung.Base`. Three files in this package that squatted the bare `Base` namespace (`IDamageable.cs`, `SkillExecutionContext.cs`, `ISingleInstanceStackable.cs`) renamed alongside it, `Base`->`Hung.Base`, consistent with their sibling contracts (`IDamageTarget`/`ILogicEffectReceiver`) they compose. `Base.Combat`/`Base.StatusEffects` (this package's own squat) intentionally NOT renamed this pass (Pass 4's territory).
### Fixed
- `DamageDealtContext.cs` (`Base.Combat`) used `ELEMENTAL` unqualified via implicit access to the old bare-`Base` namespace it was nested under; broke when `Base` became `Hung.Base` - added explicit `using Hung.Base;`.

## [0.2.0] - 2026-07-10
- Hung.Combat.Core: gained composed `Base.IDamageable` (from com.hung.base, recomposed per PvM).
- Hung.Combat: gained `AOEDamage` (moved from com.hung.base, typed-damage version).

## [0.1.0] - 2026-07-10
- Extracted Combat + StatusEffect from PetVsMonster (net-new to canon).
- Three assemblies: Hung.Combat.Core (contracts), Hung.Combat (pipeline), Hung.StatusEffect (effects/synergy).
- Retired 4 asmref injections (Base/ into Hung.Base, Data/ into Hung.Data).
- Requires Sirenix Odin (imported asset) for StatusEffect config SOs.

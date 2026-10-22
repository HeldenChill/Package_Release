# Changelog

## [0.3.7] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.combat 0.4.6 -> 0.4.7.

## [0.3.6] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.3.4] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.3.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 for the F4-IE editor prerequisite; align Combat 0.4.3.

## [0.3.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.combat 0.4.1 -> 0.4.2; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.3.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no skill API or runtime behavior changed.

## [0.3.0] - 2026-07-11
### Changed
- **Migration (B1 Pass 4 - gameplay):** package's `namespace _Game` (9 files: `BaseSkill`/`SkillData`/`ColliderController`/`EffectVFXController`/`BaseBullet`/`BaseProjectile`/`BaseThrow`/`ExplosionThrow`/`ViroBullet`) -> `Hung.Skill`. rootNamespace set on the asmdef. Bumped `com.hung.combat` dependency to 0.4.0 (Pass 4's own combat rename).
- Game-side consumers in `Hung.Skill.Game` (still `namespace _Game`, unrenamed - separate pass) lost implicit same-namespace access; added explicit `using Hung.Skill;` to `CharacterSkill.cs`, `FireBallSkill.cs`, `ImpulsoSkill.cs`, `LacerateSkill.cs`, `OrbisSkill.cs`, `PestilentiaSolumSkill.cs`, `ViroSkill.cs`, `NoviceGun.cs`, `ProcessSkillModule.cs`.
### Fixed
- `BaseBullet.cs` shadow bug: `using DesignPattern;` (bare, for `ObjectContainer`/`SkillObjContainer` which still squat that namespace in com.hung.base) resolved to the sibling `Hung.DesignPattern` package instead once nested under `Hung.Skill` (same shadow class as Pass 1/3) - fixed with `using global::DesignPattern;`.

## [0.2.0] - 2026-07-10
- BaseBullet adopts typed damage (`DamageHitBuilders.From`) per PvM; requires com.hung.combat.
- PvM's `rb.velocity` + dead-using diffs remain rejected (canon `linearVelocity` correct).

## [0.1.0] - 2026-07-09
- Extracted from PetVsMonster/Package_Repo `_Skill` folder: BaseSkill, SkillData, projectile bases, ColliderController, EffectVFXController.

# com.hung.combat

Damage pipeline + status-effect system, layer L3.

## Assemblies
- **Hung.Combat.Core** — shared contracts (DamageHit, StatusEffect base, IStatusEffectHost, IModStat, ICombatDamageable). Both modules below depend on this and nothing else of each other.
- **Hung.Combat** — DamageCalculator, DamagePipeline, DamageModifierRegistry, CompositeResistanceProfile.
- **Hung.StatusEffect** — StatusEffectManager/Factory, 9 effects, synergy resolver + reactions, 2 config SOs.

## Requirements
- **Sirenix Odin Inspector** (imported paid asset). The StatusEffect config SOs use one Odin attribute; the package will not compile without Odin present.

## Extension
The game implements `ICombatDamageable`, `IStatusEffectHost`, `IModStat` on its own character/enemy types. This package references no game code.

## Runtime verification
- `IDamageable`/`DamageHit` pipeline: runtime-verified via `DamageHarness` (`Assets/_Game/_Harness/`) — skill cast -> HP decreases.
- `IStatusEffectHost`/stat-modifier/synergy pipeline: runtime-verified via `StatusEffectHarness` (`Assets/_Game/_Harness/StatusEffectHarness/`) — 2 stacked `ColdStatusEffect` instances modify+revert a stat on expiry; a custom `Cold`+`Shock` `SynergyDefinitionData` fires an `ISynergyReaction`. See `Docs/validation/harness-log.md`.

# Changelog

## [0.2.1] - 2026-08-01
- Added generic event-channel aggregation, projectile snapshot evidence, and optional editor play-mode auto-stop after a run.
- Replaced game-specific package defaults and documentation with game-neutral contracts.

## [0.2.0] - 2026-07-29
- Added append-only runtime confidence evidence records with redaction, artifact hashing, terminal result states, and built-player command-line flags.
- Added a built-player entrypoint seam for game-owned runtime scenario runners and exit codes 0/1/3 for pass/fail/blocked confidence gates.

## [0.1.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no AutoTest API or runtime behavior changed.

## [0.1.0] - 2026-07-07
- Extracted the game-agnostic core after its seam refactor: stats hook, assertion registry, executor/snapshot factories, and pluggable ready-check.

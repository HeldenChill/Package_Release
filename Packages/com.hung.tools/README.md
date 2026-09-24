# Hung Tools (L6 dev tooling)

Editor/dev utilities. Four assemblies: `Hung.Tool` (UI flow/DOTween preview shared data), `Hung.Tool.Spine` (Spine bake setup components placed on scene GameObjects), `Hung.Tool.Editor` (editor-only asset maintenance, VFX color tone shifting, and general tooling), and `Hung.Tool.Editor.Spine` (Spine sprite-sheet/particle baker windows). GameLog diagnostics re-homed to `com.hung.diagnostics` (Ph3 Task 3.5, 2026-07-11).

Known debt: Hung.Tool references Hung.UI which is not yet a package (Phase 3) — package is only portable alongside the UI source until then.

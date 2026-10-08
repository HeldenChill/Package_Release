---
paths:
  - "Assets/**/*.cs"
---
# Rule: C# Style

**Scope:** Framework (`com.hung.framework`). Loads when a `.cs` file under `Assets/` is read. No project values.

## Naming

- `public` fields and properties on types under `Assets/`, including inspector-exposed ones: **PascalCase** — `DifficultyMode`, not `difficultyMode`.
- Private / protected fields: `camelCase`. Methods and non-serialized properties: PascalCase.
- Legacy code: do not rename fields in untouched types; match the surrounding type when editing existing code.
- Renaming a serialized `public` field: add `[FormerlySerializedAs("oldName")]` so existing `.asset` / prefab data keeps its values.

## Indentation

- **4 spaces** per level, no tabs. Class members 4, method bodies 8, nested blocks +4 each. Never 2-space indentation.
- Match the existing file's style; the repo `.editorconfig`, if present, is authoritative for Format Document.

## Blank lines (do not double-space)

Never insert a blank line after every source line — that is a formatting bug, not project style.

- Usings: one contiguous block.
- Consecutive field / property declarations: no blank line between them unless the file already groups them.
- One blank line between methods; inside methods, blank lines only between logical blocks the file already separates.

When editing, prefer a targeted replace over a full-file rewrite (full rewrites are the main source of double-spacing). After any full rewrite, re-read the file and remove spurious blank lines.

---
name: unity-csproj-sync
description: >-
  Audits Unity asmdef C# files against generated *.csproj Compile Include entries and guides
  IDE project regeneration when OmniSharp shows false errors. Use when the user reports IDE errors
  but Unity compiles fine, missing types in Cursor, csproj out of date, or asks to sync/check csproj.
disable-model-invocation: true
---

# Unity csproj sync

## Problem

Unity compiles from **asmdef + Asset Database**. Cursor/OmniSharp uses **generated `*.csproj`** with explicit `<Compile Include="...">`. Scripts added in Cursor (or moved) exist on disk before Unity regenerates csproj → **false IDE errors** (`type not found`, missing base class) while **Unity Console is clean**.

Repo policy: **do not hand-edit `*.csproj` / `*.sln`** unless the user explicitly asks. Prefer Unity regeneration.

## When to use

- User: "IDE shows errors but Unity is fine"
- After adding/moving `Assets/**/*.cs` from Cursor or CI
- Before blaming code for "missing" types — verify csproj first
- User asks to **check** or **sync** csproj

## Quick workflow (agent)

1. **Run the checker** from repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/pkg-framework-unity-csproj-sync/scripts/unity-csproj-sync-check.ps1
```

Single assembly:

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/pkg-framework-unity-csproj-sync/scripts/unity-csproj-sync-check.ps1 -Csproj <Assembly>.csproj
```

JSON for parsing:

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/pkg-framework-unity-csproj-sync/scripts/unity-csproj-sync-check.ps1 -Json
```

2. **Interpret exit code**
   - `0` — disk and csproj lists match for all asmdefs that have a root `{AssemblyName}.csproj`
   - `1` — at least one mismatch; print **Missing from csproj** and **Stale in csproj**

3. **Sync (user action, not agent csproj edits)**
   - Open **Unity Editor** on this project
   - **Edit → Preferences → External Tools → Regenerate project files**
   - Or **Assets → Refresh** (focus Unity once after external file changes)
   - Optional if present: the project's own **Sync IDE project files now** menu
   - Re-run checker until `[OK]` for affected csproj
   - In Cursor: **C#: Restart Language Server** / **OmniSharp: Restart OmniSharp** only if squiggles remain after csproj file timestamp updates

4. **Confirm**
   - Re-run checker
   - Optionally grep one previously missing file:

```powershell
Select-String -Path <Assembly>.csproj -Pattern "<File>\.cs"
```

5. **Report to user** using template below. Append handoff line when agent added scripts:

> Handoff — keep Unity Editor open briefly so IDE project files regenerate (or run checker again after Regenerate project files).

## Agent rules

| Do | Don't |
|----|--------|
| Run `unity-csproj-sync-check.ps1` when IDE/Unity disagree | Patch `*.csproj` by default |
| Tell user to regenerate in Unity when `Missing` > 0 | Tell user to restart entire IDE as first fix |
| Re-run checker after user confirms Unity regen | Assume `Assembly-CSharp.csproj` covers asmdef scripts |
| Mention which assembly/csproj is out of sync | Claim compile success from IDE alone |

**Assembly-CSharp**: Checker only pairs `{asmdef.name}.csproj` at repo root. Loose scripts under `Assets` without asmdef use `Assembly-CSharp.csproj` — extend investigation manually if needed.

## Output template

```markdown
## csproj sync audit

**Status:** In sync | Out of sync

**Affected:** <Assembly>.csproj (and others if any)

**Missing from csproj (IDE blind spots):**
- `Assets\...\File.cs`

**Stale in csproj (deleted/moved on disk):**
- `Assets\...\OldFile.cs`

**Next step:** Unity → Regenerate project files → re-run checker → restart C# language server if needed.
```

## Related repo docs

- The project's IDE-sync rule, if it has one (for example `.claude/rules/unity-ide-sync.md`)

## Troubleshooting

| Symptom | Action |
|---------|--------|
| Checker OK, IDE still red | Restart language server; confirm workspace opened via `.sln` |
| Checker missing after regen | Unity External Tools: ensure **Editor** is set to generate `.csproj` for packages |
| Real Unity Console errors | Not csproj drift — fix code/asmdef references |
| Nested asmdef | Checker excludes child-asmdef subtrees from parent counts |

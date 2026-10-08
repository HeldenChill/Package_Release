# Rule: Unity MCP Is the Default Editor Interface

**Scope:** Framework (`com.hung.framework`). Project values: `.claude/rules/project-values.md` § `unity-mcp`.

---

## Project values

Read this rule's values from `.claude/rules/project-values.md` § `unity-mcp`. Keys below appear as `<KEY>`.

---

## Policy

**Unity MCP is live in this project.** It exposes the Editor: prefab/scene read + write,
component inspection, console, play mode, tests. Use it. Do not fall back to manual
instructions when the bridge is available.

### Reads — use MCP, not YAML

To inspect a prefab or scene, call `manage_prefabs get_hierarchy` / the components resource —
**not** `Read`/`Grep` on the `.prefab` file.

Raw YAML is a *fallback*, not the default. It cannot show driven values (a
`ContentSizeFitter`-driven `RectTransform`), resolved nested-prefab state, or anything runtime.
It is legitimate only when the bridge is down (see Fallback).

### Writes — allowed, but must be verified

**Prefab and scene edits via MCP are permitted without asking.** This overrides the
"never edit prefabs, list them for the user" default in `unity-ide-sync.md` — that rule's
prohibition now applies only when MCP is unavailable.

Every write carries three obligations:

1. **Verify the write landed.** The tool reporting `success: true` is not evidence. Read the
   serialized field back (component resource, or grep the saved YAML for a non-zero `fileID`)
   and confirm. Tools have reported success on writes that silently no-op'd.
2. **Report every object and field touched**, so the change is reviewable in the git diff.
3. **`.asmdef` files remain user-approved** — MCP does not change that. Asmdef edits alter the
   dependency graph and are a layer-architecture decision (`layer-architecture-dispatch.md`).

### Runtime verification — do it, don't hedge

`layer-architecture-dispatch.md` requires saying *"compiles; runtime-unverified"* rather than
claiming a fix works. **When MCP and Unity are up, that hedge is no longer acceptable as a
default** — enter play mode, exercise the path, read the console, and report what was actually
observed.

Only fall back to "compiles; runtime-unverified" when the bridge is genuinely unavailable, and
say *why*.

Play mode has side effects (writes saves, may touch analytics/ads SDKs). If the change under
test is destructive to user data, say so and ask before entering play.

## The bridge has two halves

```
Claude Code  →  MCP server (uvx)  →  Unity Editor
```

`/mcp` reporting "connected" only proves the **first** hop. If a tool returns
`No Unity Editor instances found`, the *Editor* side is down — the server is fine.

**Cause is almost always: the Unity Editor is not running.** Unity Hub alone is not enough
(Hub is a launcher; the bridge lives in `Unity.exe`).

### Fallback when the bridge is down

1. Say so plainly — do not silently degrade and let the user think MCP was used.
2. Ask the user to open the project in the Unity **Editor** (not just Hub).
3. If they decline or it cannot be brought up: read the `.prefab` YAML directly, state that the
   read is static-only, and revert to `unity-ide-sync.md` behaviour (list prefab changes for
   manual application). Runtime claims revert to "compiles; runtime-unverified".

## Tool gotchas

- **Asset-scope IDs don't resolve.** Instance IDs from `manage_prefabs get_hierarchy` are NOT
  usable in `manage_components` or component resources. Call `open_prefab_stage` first, then
  `find_gameobjects` for live scene IDs.
- **Inactive GameObjects are unfindable** — `find_gameobjects` has no `search_inactive`.
  Temporarily `set_active: true`, grab the ID, wire it, restore the inactive state.
- **Editing a `.cs` triggers a domain reload.** The bridge drops mid-call
  (`Connection closed before reading expected bytes`), and the next call may return
  `Unity is reloading; please retry`. Both are normal — retry, then `read_console` to confirm
  the compile.

## Subagents do not have MCP

Spawned agents get their own tool set and **cannot call MCP tools**. Any task requiring Editor
access — prefab wiring, play-mode verification, console reads — must be done **inline on the
main thread**, or the agent must hand the Editor work back rather than guessing at it.

## Related rules

- `unity-ide-sync.md` (where the project defines it) — the manual-prefab default this rule overrides when MCP is up; `.asmdef` approval still stands
- `layer-architecture-dispatch.md` (where the project defines it) — the "runtime-unverified" honesty rule this rule upgrades
- [pkg-framework-compile-verification.md](pkg-framework-compile-verification.md) — `dotnet build` gate; MCP's `read_console` is the in-Editor equivalent

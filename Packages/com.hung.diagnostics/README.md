# Hung Diagnostics

## Purpose

Structured runtime log capture (`GameLog`) with a JSON file sink and an Editor viewer window. Re-homed out of `com.hung.tools` (2026-07-11) so a consumer can get logging without pulling in the rest of the editor-tooling package. It is not a general logging framework — it is a fixed-schema record/scope/sink pipeline purpose-built for this framework's diagnostics.

## Layer + dependencies

- Layer: `0` (see `Docs/framework-catalog.yaml`)
- Dependencies: none — zero `com.hung.*` refs, `GameLog`/`GameLogConfig`/`GameLogRecord` etc. are fully self-contained aside from Unity's bundled `Newtonsoft.Json` package.

## Prerequisites

None beyond Unity's built-in `com.unity.nuget.newtonsoft-json` package (ships with the editor, auto-referenced).

## Quick start

`GameDiagnosticsBootstrap` auto-creates itself via `[RuntimeInitializeOnLoadMethod]` and calls `GameLog.Initialize(config)`, loading `Resources/GameLogConfig.asset`. Call `GameLog.Info/Warning/Error(category, message, ...)` from anywhere once initialized. In an isolated scene that skips the normal boot chain (e.g. a test harness), call `GameLog.Initialize(ScriptableObject.CreateInstance<GameLogConfig>())` manually first.

## Public API index

- `GameLog` — static entry point: `Initialize`, `Trace`/`Debug`/`Info`/`Warning`/`Error`/`Fatal`.
- `GameLogConfig` — `ScriptableObject` config asset (min level, file sink settings).
- `GameLogRecord` — one structured log entry.
- `GameLogScope` — disposable scoped-category helper.
- `JsonFileLogSink` — writes records to a JSON file on disk.
- `GameDiagnosticsBootstrap` — auto-init `MonoBehaviour`, `RuntimeInitializeOnLoadMethod`.
- (Editor) `GameLogViewerWindow` — browses captured JSON log files.

## Known limitations / sharp edges

- `GameLog.Write` is guarded for off-main-thread calls (main-thread-only APIs like `Time.realtimeSinceStartup` are captured via a cached thread ID) — found+fixed as a real boot-flow bug during Phase 6, see `canon-decisions.md`.
- `GameLog.Initialize` is idempotent (`if (initialized) return;`) — **first caller wins for the whole play session**, config included. A harness placed inside a scene reached via the real boot chain (e.g. GameScene) will find GameLog already initialized with the production config (typically `echoToUnityConsole=false`) by the time the harness's own `Awake` runs — its own `Initialize(...)` call is then a silent no-op and none of its `GameLog.Info` calls reach the console, even though the logic they're reporting on ran correctly. Found during the B3 StatusEffectHarness build (2026-07-11). No reset/reconfigure API exists. Workaround used: add a plain `Debug.Log` alongside any `GameLog.Info` call a harness needs to be console-visible for — `Debug.Log` isn't gated by GameLog's config at all.
- No automated tests yet.

## Samples

None.

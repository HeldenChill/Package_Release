# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to pre-1.0 SemVer convention: breaking changes bump the
minor version, and carry a `Migration` note.

## [0.2.2] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no diagnostics API or runtime behavior changed.

## [0.2.1] - 2026-07-11
- **Fix:** `JsonFileLogSink.Dispose()` double-disposed the `StreamWriter` — `WriteLoop()`'s own exit path already flushes+disposes `writer` as its last action once `running` goes false, but `Dispose()` then waited via `workerThread.Join(1000)` and unconditionally flushed+disposed the same writer again, throwing `ObjectDisposedException: Can not write to a closed TextWriter` on every clean shutdown (`GameDiagnosticsBootstrap.OnDestroy` -> `GameLog.Shutdown` -> here). Fixed: when `Join` actually catches a live worker thread, `Dispose()` now trusts `WriteLoop`'s own cleanup and returns without touching `writer` again; the manual flush+dispose only runs for the case where the worker thread was never alive to begin with.

## [0.2.0] - 2026-07-11

### Changed

- **Migration (B1 Pass 3 - base, deviation from stale plan note):** `Base`->`Hung.Diagnostics` (6 Runtime files), `Base.EditorTools`->`Hung.Diagnostics.Editor` (3 Editor files). rootNamespace updated on both asmdefs. The stageBC plan's Pass 6 note ("GameLog renames to Hung.Tool family") predates this package's Ph3 extraction from `com.hung.tools` and is superseded - both asmdefs' own `rootNamespace` was still literally `"Base"`, so per the `assembly Hung.X => namespace root Hung.X` rule this package claims its own rename now rather than waiting for Pass 6.

## [0.1.0] - 2026-07-11

### Added

- Initial extraction from `com.hung.tools` (Ph3 Task 3.5): `GameLog` + config/record/scope/sink + Editor viewer window, verbatim, GUID-preserved.

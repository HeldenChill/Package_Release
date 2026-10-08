# Rule: Compile Verification Gate

**Scope:** Framework (`com.hung.framework`). Project values: `.claude/rules/project-values.md` § `compile-verification`.

---

## Project values

Read this rule's values from `.claude/rules/project-values.md` § `compile-verification`. Keys below appear as `<KEY>`.

---

## Policy (universal for Unity — do not change)

Unity-generated `.csproj` files at the project root allow compiling any single assembly with `dotnet build` — no Unity editor needed. Missing `using` directives and missing asmdef references surface as `CS0246` immediately. This gate is **mandatory for every code change**.

### Agent self-gate (every layer agent, every code task)

1. After editing, run `dotnet build` for **every assembly you touched** — one invocation per assembly:
   ```
   dotnet build <Assembly>.csproj -v q --nologo
   ```
   If you edited files in two assemblies (e.g., `Hung.Character` and `Hung.Gameplay`), run both builds.

   **First confirm the project actually contains your edited file** — `grep -c "<YourFile>.cs"
   <Project>.csproj`. A `0` means the file is an orphan in `Assembly-CSharp` and this build will
   pass without ever seeing your edit. See "The orphan-file trap" below.
2. If errors: fix them **in the same session** — you have the edit context; do not report back with a broken build.
3. A report is **invalid** unless it includes one build-evidence line **per built assembly**:

```
Build evidence: <Assembly> — 0 errors (<N> warnings)
```

The main thread rejects any report missing these lines and re-dispatches.

### Main-thread integration gate (multi-layer tasks only)

After integrating all agents' work on a task that touched 2+ layers, the main thread runs **once**:

```
dotnet build <L5_CSPROJ> -v q --nologo
```

(`<L5_CSPROJ>` is the `project-values.md` value — angle brackets, NOT `{{ }}`, so `sync.ps1` never mistakes it for an unfilled token.)

This transitively compiles the full assembly chain and catches cross-assembly seam errors no single agent's build covered. On errors: re-dispatch the owning agent with the compiler output pasted verbatim. Single-layer tasks skip this — the agent's own gate suffices.

### The orphan-file trap

**A `.cs` file under `Assets/` is NOT necessarily in its layer's csproj.** Unity assigns a
file to an assembly by the nearest `.asmdef`/`.asmref` *above it in the directory tree*. With none,
the file falls into the predefined `Assembly-CSharp` — invisible to every layer csproj.

The project's current orphan list, if it keeps one, is in `project-values.md` § `compile-verification` (`ORPHAN_FILES`).

**Before quoting build evidence for a file, confirm the project you built actually contains it:**

```
grep -c "<YourFile>.cs" <Project>.csproj      # 0 = your build never saw it
```

If the count is 0, the file is an orphan — build `Assembly-CSharp.csproj` instead and say so in the
evidence line. A green build on a project that excludes the edited file is a **false negative**, not
verification.

### Caveats

- **csproj files are Unity-generated.** After any `.asmdef` change, Unity must regenerate projects before builds are trustworthy (keep Unity open — see `unity-ide-sync.md`). Never hand-edit csproj files.
- **Warnings are not gated.** Only errors block. Do not "fix" pre-existing warnings outside task scope.
- **Compile ≠ fixed.** This gate removes compile-error round-trips; runtime honesty rules still apply — say "compiles; runtime-unverified" and list play-mode steps.
- New `.cs` files created outside Unity are not in the csproj until Unity refreshes. If the build cannot see a newly created file, state this in the report and list Unity refresh as a required user step.

## Why

The most common dispatch failure was an agent writing locally-valid C# that referenced a type from an assembly the target asmdef does not reference, or missing a `using`. Static review cannot catch this; the compiler catches it in seconds for zero context tokens.

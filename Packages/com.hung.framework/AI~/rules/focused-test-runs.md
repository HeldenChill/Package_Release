# Rule: Focused Test Runs

**Scope:** Framework (`com.hung.framework`). Project values: `.claude/rules/project-values.md` § `focused-test-runs`.

---

## Policy

Default to running only the tests relevant to the current change — scope `run_tests` with
`test_names`, `group_names`, or `assembly_names` matching the touched classes/namespaces.

**Do NOT trigger the full `<EDITOR_TEST_ASSEMBLY>` assembly** (900+ tests, multi-minute run,
GC/reload churn) as a default verification step. Reserve unscoped full-suite runs for designated
regression checkpoints:

- A plan's explicit final regression task (e.g. "run the full NUnit suite once").
- Immediately before a merge/PR.
- When the user explicitly asks for a full-suite run.

## Why

The project's compile gate (`pkg-framework-compile-verification.md`) plus a narrowly-scoped test run already
prove an incremental change is correct. Running the full suite after every small edit wastes
minutes per invocation for no additional signal, and large runs can make the MCP bridge go quiet
for minutes (normal, but still a cost). Full-suite runs earn their cost only at real regression
gates, not after every group of changes within a task.

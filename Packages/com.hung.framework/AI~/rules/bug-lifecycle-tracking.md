# Rule: Bug Lifecycle Tracking

**Scope:** Framework (`com.hung.framework`). Project values: `.claude/rules/project-values.md` § `bug-lifecycle-tracking`.

## Project values

Read this rule's values from `.claude/rules/project-values.md` § `bug-lifecycle-tracking`. Keys below appear as `<KEY>`.

Configure `BUG_LEDGER` and optionally `BUG_RECORDS_DIR`:
- **Single ledger (legacy):** `BUG_LEDGER = .cursor/memory/mem-known-bugs-index.md`
- **Domain collection:** `BUG_LEDGER = .cursor/memory/bugs/README.md` and `BUG_RECORDS_DIR = .cursor/memory/bugs`

In collection mode, search the entire collection before filing. Each bug record resides in its owning domain file (`mem-bugs-<domain>.md`). Exactly one authoritative record per ID across all domains; status transitions update the record in-place without moving it between files. Historical identity mappings are preserved permanently under `bugs/history/`.

Validation in collection mode requires the shared validator CLI (`.claude/tools/ai-audit/validate-bug-memory.js`). Run `sync.ps1 pull` to install or update shared tools.

## Policy

Record every credible bug or potential bug found during development, review, testing,
debugging, or runtime verification in `BUG_LEDGER` during the same session.

A credible finding requires at least one concrete source: reproducible behavior, failing
test, exception or log, user report, or precise risky code path with plausible impact.
Do not record unsupported speculation.

Give each new record an ID of the form `BUG-YYMMDD-xxxx` (date filed + 4 random lowercase hex):
`node -e "console.log('BUG-'+new Date().toISOString().slice(2,10).replace(/-/g,'')+'-'+require('crypto').randomBytes(2).toString('hex'))"`.
Never compute "max + 1": two machines filing the same day collide. Existing `BUG-NNNN` IDs are frozen,
never reused or renumbered except to repair a collision. Search the ledger before creating a record;
duplicates use `DUPLICATE` and reference the canonical ID.

Allowed types: `bug`, `risk`, `gap`, `planned`.
Allowed severities: `critical`, `high`, `medium`, `low`, `unknown`.
Allowed statuses: `SUSPECTED`, `CONFIRMED`, `IN_PROGRESS`, `VERIFY_PENDING`, `RESOLVED`,
`REJECTED`, `DEFERRED`, `WONT_FIX`, `DUPLICATE`.

Lifecycle: `SUSPECTED` becomes `CONFIRMED`, `REJECTED`, or `DUPLICATE` after investigation.
`CONFIRMED` may become `IN_PROGRESS`, `DEFERRED`, or `WONT_FIX`. A completed fix becomes
`VERIFY_PENDING`; only relevant passing automated or runtime verification permits `RESOLVED`.
Failed verification returns the item to `CONFIRMED` with updated evidence.

## Status cell formatting (required)

The `Status` cell is **not** written as bare text. Wrap the status word in its marker span so
the ledger colour-codes in Markdown Preview Enhanced:

```markdown
| BUG-0150 | bug | high | <span class="st-resolved">RESOLVED</span> | ... |
```

| Status | Span class | Rendered colour |
|---|---|---|
| `RESOLVED` | `st-resolved` | green |
| `VERIFY_PENDING` | `st-verify` | green, lighter |
| `CONFIRMED` | `st-confirmed` | red |
| `SUSPECTED` | `st-suspected` | yellow |
| `DEFERRED` | `st-deferred` | grey |
| `IN_PROGRESS` | `st-inprogress` | blue |
| `DUPLICATE` | `st-duplicate` | purple |
| `REJECTED` | `st-rejected` | grey |
| `WONT_FIX` | `st-wontfix` | grey |

Apply this on **every** row write — new records and status transitions alike. When changing a
status, replace the whole span (class *and* text); a mismatched pair renders the old colour
against the new word.

Colours are defined once in the MPE global stylesheet `~/.crossnote/style.less` (on Windows,
`%USERPROFILE%\.crossnote\style.less`). That file styles the text only — no row background.
Do not put colour in the ledger itself.

### Constraints

- The table must stay a **real Markdown table**. Only the Status cell may contain HTML; never
  convert rows to `<tr>`/`<td>`.
- Never leave a blank line between rows — it splits the table and breaks preview rendering.
- Keep the status word verbatim inside the span, so text search and
  `grep '>CONFIRMED<'` keep working.

## Ledger integrity

- One record per ID. Two rows sharing an ID is a defect in the ledger itself — reassign the
  newer row to the next unused ID rather than editing either record's content.
- Before filing, check whether an existing row already covers the same defect. If one does,
  mark the narrower row `DUPLICATE`, name the canonical ID, and carry any newer evidence up
  into the canonical row so nothing is lost by merging.

Every state change updates `Updated`, `Evidence`, and `Resolution` as applicable. Terminal
states require reasons. `DUPLICATE` requires canonical ID. `RESOLVED` requires verification
evidence. Keep resolved and rejected records permanently.

At task end report new IDs, changed IDs, and current-task `VERIFY_PENDING` IDs. If none,
state `Bug ledger: no changes`.

## During debugging

Whichever debugging workflow runs (default `superpowers:systematic-debugging`): a credible but
unconfirmed cause enters as `SUSPECTED`; the confirmed root cause becomes `CONFIRMED`; active
implementation becomes `IN_PROGRESS`; an applied fix becomes `VERIFY_PENDING`; only relevant
passing verification becomes `RESOLVED`. Never delete the record.

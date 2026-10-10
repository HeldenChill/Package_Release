# com.hung.framework

Doc-only. Holds the Framework-scope AI config every ComHung project shares:

| Folder | Loaded | Content |
|---|---|---|
| `AI~/rules/` | always (as `.claude/rules/pkg-framework-*.md`) | compile-gate, MCP, test, bug-ledger, C# style rules |
| `AI~/knowledge/` | on demand (`.cursor/pkg-ai-index.md`) | what is true and why |
| `AI~/playbooks/` | on demand | step procedures |
| `AI~/tools/`, `AI~/skills/`, `AI~/hooks/` | as `.claude/tools/pkg-framework-*`, `.claude/skills/pkg-framework-*`, `.claude/hooks/pkg-framework-*` | scripts and skills |

Project-specific values these rules need live in the project's `.claude/rules/project-values.md`.
Edit here, bump the version, release; never edit the generated copies in a project.

Unity MCP guidance names optional Project rules as plain references; hosts need not define those files for package rule links to resolve.

Ledger validation script alidate-bug-ledger.ps1 delegates to the project's installed shared validator .claude/tools/ai-audit/validate-bug-memory.js when available, enabling collection-mode validation across multiple domain bug memories. Hosts using single-file legacy mode retain local PowerShell validation fallback.

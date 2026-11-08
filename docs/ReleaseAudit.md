# WSA-RT-016_windows_beta_hardening audit

Audit date: 2026-11-07. Baseline: `4d67106`. Windows repository and the Core and
Docs repositories were clean before work. No applicable AGENTS.md was found in
the workspace or its user/Code ancestors. The macOS repository is read-only reference.

## Evidence before implementation

- One WPF .NET 9 project, one solution, no automated C# tests, no installer or
  update implementation. Existing build/bootstrap/run scripts are retained.
- `scripts/build_app.ps1`: build passed, zero warnings/errors.
- `scripts/run_app.ps1 -SkipBuild -SkipBootstrap`: launched. Real desktop UI
  Automation found the 560x68 toolbar and nine buttons. This proves startup,
  not every feature advertised in historical documents.
- `scripts/test_core_cli.ps1`: sandbox Swift build failed but script returned 0
  and executed an old binary. With cache access the actual Swift build succeeded
  and returned real Danish suggestions. Exit checks need correction.
- Core resources contain a 95 MB Danish legacy SQLite pack (schema 0, no
  `pack_meta`) and an empty English file. File existence alone incorrectly marks
  English available. No ws-lang-da checkout was found.
- Provider requires adjacent source repositories even after installation, writes
  full context to temporary files, lacks a timeout and does not kill cancelled
  processes. CLI JSON arrays are an existing unversioned contract.
- Editor is inside borderless topmost toolbar; selection offsets mishandle
  paragraph breaks; requests use document end instead of caret context.
- Mouse accepts go directly to local editor while global hotkeys use a different
  external commit path. External SendInput excludes only the toolbar HWND, not
  the original target; its INPUT union is undersized on x64.
- External token diagnostics contain raw typed words. Insights store typed and
  accepted words regardless of the sentence-example privacy preference.
- TTS, STT, OCR, Word COM selection import, settings and insights have substantive
  implementations and are preserved. Their historical PASS labels do not prove
  end-to-end operation on this machine today. Login/backend is not implemented.

## Decision

| Option | Assessment |
|---|---|
| Continue unchanged | Keeps code but leaves focus, commit, deployment and privacy defects; rejected. |
| Controlled WPF refactoring | Selected. Retains Core, SQLite format, rich editor, native controls, speech/OCR/Office adapters and history; permits explicit shell/adapters and contract tests. |
| New solution/WinUI | Would discard working adapters, duplicate migration/validation work and does not resolve external editor trust; no measured benefit. |

The three layers remain shared Core/pack boundary, WPF orchestration, and isolated
Windows adapters. Native Windows accessibility comes from normal editor controls
and named actions. External commits must fail closed for unsupported surfaces;
UIA geometry alone is not authorization to inject keyboard events.

## Conflicting references

The 2026-04-17 WSA-UX-012 handoff supersedes the 2026-04-15 expandable-toolbar
contract. A separate normal activating editor is required. The older WinUI
recommendation predates the functioning WPF implementation. Legacy pack support
is explicitly retained without inventing metadata or changing the shared pack.
Modern packs must validate their metadata; legacy packs cannot claim modern pack
parity. macOS screenshots/recordings are product references, not executable tests.

The May 9 language-pack contract still names schema 9, while the pinned Core
PackBuilder emits schema 14. The Windows validator accepts the reviewed 8–14
query profile with required parity columns, rejects unknown newer revisions and
does not rewrite packs. Modern-pack tests use synthetic schema-14 data; the real
bundled DA input remains the explicitly supported legacy schema-0 file.

## Release gate

A signed beta requires a publisher certificate, trusted HTTPS release endpoint,
qualified app/DPI/accessibility tests and clean-machine installation/update tests.
No unsigned production update feed may be enabled. .NET 9 reaches end of support
on 2026-11-10; use .NET 10 LTS for release (Microsoft support policy).

Final test evidence, remaining gates and changed-file inventory are recorded in
the dated delivery note. This audit does not label the app release-ready.

# WSA-RT-032_windows_exe_beta_distribution

ID: WSA-RT-032. Owner: Windows. Date: 2026-11-15.

## Scope and decision

User reprioritisation: park G05, provide a conventional installer, then review
Windows/macOS visual and functional parity. Retain WPF, Core CLI, packs, editor,
sidebar Settings and Windows adapters. Inno Setup extends `package_app.ps1`;
EXE checks for signed updates and prompts before installer handoff. No Core,
macOS, backend or shared Docs repository changes.

| Slice | State | Evidence / remaining work |
| --- | --- | --- |
| 0: distribution identity and isolated preview | Implemented | Bounded marker, separate data/mutex, bundled Core-only resolution |
| 1: per-user installer and upgrades | Local qualification PASS | Six actual standard-user install/upgrade/Core/DA/retention/downgrade/uninstall checks |
| 2: update detection, confirmation and trust | Implemented; contracts PASS | 38 checks; real Authenticode positive/negatives, synthetic network/launcher |
| 3: runbook and backend handoff | Implemented | [Release procedure](../ExeBetaRelease.md), [backend prompt](../handoffs/WSA-RT-032_exe_beta_backend_agent_prompt.md) |
| 4: public beta | External qualification outstanding | Approved signing, backend extension, uploader alignment and real HTTPS update cycle |

## Measured validation

- Full solution built into fresh `.artifacts/exe-final-clean-build-20261115` outputs:
  zero warnings/errors. This verifies fresh outputs, not a new clone/network bootstrap.
- EXE updater: 38 PASS; downloaded vendor compiler verified with real Windows
  Authenticode, exact subject; wrong subject/unsigned rejected. Synthetic fixtures
  cover metadata, tamper, stale offer, trust recheck and one-use handoff. A real
  WordSuggestor production update has not been downloaded or installed.
- Native EXE: 6 PASS with preview `ae63f10c4972`, N `0.2.0.1`, N+1 `0.2.0.2`;
  qualification `.artifacts/exe-tests/ece50576a30347c4b8164fd312f9a5a7/qualification.json`.
  No elevation/trust import; uninstall retains isolated settings/user words.
- Existing application: 105 PASS; existing MSIX updater state: 38 PASS;
  release scripts: 203 PASS; Settings layout/accessibility: 1201 PASS at actual
  DPI 96 with simulated raster scales. Self-contained EXE payload outside the
  workspace: 55 GUI PASS, including actual Notepad/mouse/keyboard/Unicode commits,
  toolbar focus, tab/draft restart retention and diagnostic privacy; one monitor,
  actual DPI 96. This is not a production-signed installed EXE update test.
- Production-only OCR template branch: compiler PASS; never installed or registered.
- Changed-file Markdown links, exact change-note file inventory, private-key/token
  patterns and generated signing/binary artifact checks reviewed before commit.
- A transient WPF intermediate-file lock interrupted one build; sequential retry
  passed. No app/process was force-killed and no expectation weakened.

## Human beta review

Use a fresh preview installer. Compare against canonical macOS `ba18b45` and the
11 supplied Settings screenshots. This checklist remains **human NOT RUN** until
the user reviews it; automated layout/GUI evidence is narrower.

| Flow | Expected Windows behavior | Human status |
| --- | --- | --- |
| Installer/start menu/uninstall | Ordinary per-user wizard, named preview, retained data | Native automated PASS; human review pending |
| Toolbar/editor/tabs | Compact nonactivating toolbar; normal editor; independent tabs | Review pending |
| Suggestion overlay | Core Danish candidates; Ctrl+1..0/mouse same commit; safe caret fallback | Review pending |
| Profile and General | Same semantics; native Windows controls and update version/status | Review pending |
| Speech, Suggestions, Text analysis | Sidebar grouping and meaningful parity; no macOS API imitation | Review pending |
| Learning, Troubleshooting, Advanced | Preserve existing features and truthful unavailable states | Review pending |
| Windows Hello | PIN/biometry platform counterpart | User-deferred; no capable test hardware |
| Multiple displays/DPI/screen reader | Native navigation, readable focus, no focus theft | Physical multi-DPI and spoken screen-reader review not run |

## Next action

Install the preview and use editor/Settings alongside macOS screenshots. Record
specific layout/behavior differences for the next small Windows slice. The
backend agent can meanwhile implement EXE hosting from the supplied prompt.
Public release remains blocked by signing/backend qualification; G05 stays
parked and is not required for the new EXE route.

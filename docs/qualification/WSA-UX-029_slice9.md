# WSA-UX-029 slice 9 — Advanced and regression qualification

Date: 2026-11-12. Windows base `92f2ed5f1044dc69d3484294cf9f4933b4156323`;
Core `56aba63042796b2a6c398828c3737ab0cb02eae5`; macOS reference
`ba18b451372dd2e3f125e557c5db45894f4e1ae3`; backend unchanged `2f67ddc6`.
See [runtime/privacy contract](../Diagnostics.md) and
[72-control current implementation/manual matrix](settings-parity-status.v1.json).

## Audit and architecture decision

Retain WPF/.NET 10, Core CLI and existing language packs. Four Advanced flags were
persisted but disabled and had no runtime collectors; panel state and unknown-word
controls were absent. Core deliberately suppressed raw logging. Add Windows-owned
typed bounded diagnostics and a minimal additive Core-owned opt-in metadata schema.
Do not duplicate lexical logic or enable free-form logger output. macOS reads were
limited to pinned AppState/SettingsView/EngineFactory semantics; no macOS edits.

The CLI has no unknown-word lookup/handoff operation. A06/A07 remain visibly
unavailable with an explicit reason, not disguised manual user-word insertion.
The 72-row status matrix is current implementation tracking; the original
settings-controls.v1.json remains immutable slice-0 baseline history. Automated
existence/mapping is not full functional/human/release qualification.

## Targeted evidence

Core executable gate: 354 checks plus 6 real Danish parity fixtures PASS. Windows
targeted build: 0 warnings/errors. AdvancedUi: 31 PASS (21 state/storage/privacy
and 10 native UI checks). Actual mouse, Space and UIA Toggle persist independently;
screen-reader peer names, DA/EN status, external settings refresh, separate legacy
and three-way merge were checked. Explicit dynamic labels fixed an initial peer-
name failure. The old future-version fixture now uses CurrentVersion+1 so schema
6 migration does not weaken future-data rejection. No HelloNative was executed.

The intermediate broader Settings run passed Advanced 31, Update 37, Account 55,
Analysis 39, Suggestions 48, Speech 102 and Core adapter 58 before identifying
that stale future-schema fixture. Final exact-source evidence below supersedes
intermediate counts and is completed before Windows commit.

## Manual/native matrix

| Flow | Evidence / boundary |
|---|---|
| Schema 5→6; byte-exact backup; false/unknown consent | Targeted state PASS; old user data untouched |
| Independent App/placement/panel/Core/performance filtering | Worker/state PASS; no free-form message API |
| Timing count/total/max; queue/rate bound; rotation/retention | Targeted controlled tests PASS; no latency SLA |
| Disabled collection; queued epochs; teardown | State tests and source review; one in-flight metadata append may finish |
| Write error | Real file-as-directory failure PASS; bounded health code, no content/error forwarding |
| Core summary/old protocol/failure | Actual engine + synthetic stderr canary; closed fields/bounds validated |
| Checkbox mouse/Space/UIA and names | Targeted real WPF PASS; no human Narrator signoff |
| Legacy conflict and persistence | Native separate dialog save PASS; unknown/newer fields retained |
| Actual local/external panel/caret/commit traces | Fresh GUI PASS; typed NativeEdit route and independent panel/Core traces; owned Notepad only |
| A06/A07 unknown-word lookup/sharing | UNAVAILABLE, no Core operation/provider/consent/upload |
| All 72 controls | Complete mapping; individual implementation and human gaps remain explicit |
| Hello PIN/biometric | DEFERRED BY USER, not executed |
| G05 positive installed mutation / signed MSIX N→N+1 | Still BLOCKED; no trusted installed fixture/production sign-off |
| Human Narrator/contrast/DPI/multi-monitor | Not run; automated input/one host does not close these |

## Final exact-source regression — 2026-11-12

Tested staged Windows tree: `86e8e8faee19f8b3838e4c8fe8ce720415478af9`.
Committed Core: `56aba63042796b2a6c398828c3737ab0cb02eae5`.
SHA-256 of sorted non-document index entries:
`f6ed0cb876ae2e40c8921c63a408bcf6b520e687c2a556302f4279527a529f09`.
SHA-256 of all testable entries (also includes docs JSON/fixtures; excludes only
root README and docs Markdown prose):
`eb444c57e5ac565a142e1c7c818a6d11659c277ba5fd579c67c9fc6fd9a6dc17`.
Only prose documentation changed after the run. Final staged inputs must match
both fingerprints. Windows and Core were exported into fresh sibling directories;
no working-tree sources, binaries or caches were copied. App DLL and Core CLI were
verified inside that export. Ignored `.artifacts` retains logs/results, native
fixture files and screenshots; generated binaries are not committed.

| Repository gate | Concrete final result |
|---|---|
| `scripts/build_app.ps1` | PASS; 0 warnings/errors |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS; 354 Core checks + 6 real DA fixtures; existing Swift/pkg-config warnings retained |
| `scripts/test_app.ps1 -SkipBuild` | PASS; 67 |
| `scripts/test_settings_app.ps1 -Mode All -SkipBuild` | PASS; Advanced 34 (24 state/privacy + 10 native UI), Update 37, Account 55, Analysis 39, Suggestions 48, Speech 102, adapter 58, State 63, UI 58, Speech UI 24 |
| `scripts/test_gui.ps1 -SkipBuild` | PASS; 37; real owned Notepad and internal editor; DPI 96, one monitor |
| `scripts/test_media.ps1 -SkipBuild` | PASS; 3: actual OCR, synthetic STT wire, actual TTS completion; no human audible/microphone signoff |
| `scripts/test_settings_qualification.ps1` | PASS; 41 including current 72-control mapping and explicit gaps |
| `-Mode UpdateNative` | BLOCKED; exit 3 prerequisite reminder; no installed policy mutation was run |
| `HelloNative` | DEFERRED BY USER; not executed |

The actual app's owned runtime files contain hidden/local/external panel states,
safe Core summaries and NativeEdit verified route, with App debug off. They are
bounded and exclude fixture text/file paths. New process-failure/missing-Core tests
exclude private stdin/stderr canaries, paths and identities; direct DTO tests
reject missing/unknown/free-form fields and invalid counters. Old-epoch timings
are discarded after settings changes, and background write completion cannot
overwrite a later Disabled health state.

The earlier slice-8 disconnected-session UI/black-OCR failures remain valid
historical records. This active-session fresh-source All/GUI/Media PASS supersedes
those shared-flow restrictions for the source above. It does not qualify actual
MSIX installation/update, native Hello, extra DPI/monitors or human accessibility.

Review before commit: 36 documented Windows text/source files; 226 untouched
tracked baseline files and all 18 original untracked hashes preserved. Separate
HEAD-only projections exclude existing tabs/restart/release hunks. The Core commit
contains only its six documented files. No added secrets, private keys, binaries,
machine-specific paths or unrelated work; added local document links resolve.
Backend/macOS/shared Docs remain unchanged. All seven software gates pass;
full 72-control parity and public release remain **open**, not blanket-approved.

## Reproduction from Windows repository root

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode AdvancedState -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_qualification.ps1
```

Run GUI/All only in an active unlocked ordinary-user session. All excludes
HelloNative and packaged policy mutation; it reads availability and uses synthetic
backend/crypto/storage fixtures without enrolling or requesting PIN/biometrics.
Native update authority remains a separately provisioned fixture test from
[the update contract](../WindowsUpdates.md).

## Remaining work in product terms

Users can select diagnosis categories and keep logs local without document text.
The next acceptance lane verifies real accessibility/scaling and trusted installed
updates/data retention. Unknown-word controls need a Core-owned lookup/provider
contract before they can perform work; no automatic learning/upload is permitted.
Hello stays parked until the user has suitable access. Keep full sprint/release
status open until the remaining native and human requirements are satisfied.

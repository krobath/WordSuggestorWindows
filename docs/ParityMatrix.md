# Windows evidence-based parity matrix

Updated 2026-11-10 (Settings slice 1 shell qualified; later functional slices open). Source: baseline audit at Windows `4d67106`, current source,
real Core contract tests and real-desktop smoke. Historical "Done" labels mean code
was delivered, not all target programs or accessibility modes were verified.

| Capability | Baseline finding | Current implementation/evidence | Remaining qualification |
|---|---|---|---|
| Native shell | WPF/.NET 9 built; 560×68 toolbar observed | WPF/.NET 10, compact non-activating toolbar; desktop startup/focus checks pass | Human visual approval |
| Editor window | Shared topmost toolbar/editor conflicted with new handoff | Separate normal activating editor; actual local edits/Unicode/paragraph/selection checks pass with Notepad open | Extended document/file-dialog checks recorded in delivery note |
| Core reuse | Real CLI but failed builds could return success/stale binary | Versioned stdin JSON, timeout/cancellation/process reap, generation guards; real fixtures and failure-host tests pass | Mac-machine fixture execution |
| Danish pack | Legacy schema 0, no pack_meta | Read-only integrity/schema validation; explicit DA legacy profile; real suggestions pass | Metadata-complete modern DA artifact |
| EN/DE | Empty EN file incorrectly considered installed; DE absent | Empty/corrupt/wrong-language/future schema rejected; routing preserved | Actual compatible EN/DE packs |
| Suggestions/paging | Separate overlay, 10/page, 4 pages | Shared accept path; stale/duplicate results rejected; real mouse/Ctrl+1 pass | Broader keyboard/screen-reader coverage |
| Caret/selection | End-of-document requests; paragraph offsets/selection fragile | Context before caret, UTF-16 editing transaction, actual multiline emoji/Danish and selection checks pass | Long-document editing/undo soak |
| Follow/static placement | Mouse/approximate bounds treated as caret; primary-screen clamp | Confirmed caret only, static fallback, persisted physical position and per-monitor geometry | Real mixed DPI/multiple displays; one 96-DPI display tested |
| External Notepad | Optimistic foreground SendInput; mouse used local path | Snapshot-bound native Edit/RichEdit commit, verified resulting text/caret; real mouse/Ctrl+1/clipboard checks pass | >60,000 UTF-16 units unsupported; more Notepad versions |
| Word/Outlook/browser commits | Generic route present without E2E qualification | Unsafe route removed; those targets fail closed for suggestion commits | Isolated qualified adapter per host; no generic injection |
| Selection import | UIA, Word COM, automatic clipboard fallback | UIA/Word COM retained on explicit actions; no passive full-selection poll; explicit snapshot-qualified clipboard import | App-specific import matrix remains mostly untested |
| Settings/data | Local JSON; no backup/future-schema protection | Same data location/schema, atomic replace/backup, future schema protection; restart tests pass | Signed upgrade/uninstall tests |
| Settings UI/content against macOS beta.9 | Horizontal categories; no Profile/Learning categories, speech mixed into General; many controls missing or inactive | [Slice 1](qualification/WSA-UX-029_slice1.md): eight vertical categories, grouped rows/help and non-recursive Windows legacy; 20 state and 37 interactive checks | Full 72-control functional parity, live save/localization and human accessibility/DPI review open; Hello DeviceNotPresent and OS-update writes 0x800706F7 remain positive qualification gates |
| User words | No Windows persistence/management | Explicit user-word UI, Core overlay format/normalization; restart/future-schema tests pass | Shared learning/profile UI parity |
| Diagnostics/insights | Raw typed words/window titles; examples always recorded | Typed key capture replaced; stage/outcome diagnostics; stderr omitted; examples off by default; canary checks pass | Retention/rotation and opt-in usage statistics design |
| TTS | Substantive OneCore/SAPI and highlight implementation | Preserved; sanitized diagnostics; see media/manual smoke evidence | Human hearing/highlight timing across voices |
| STT | Local speech bridge, historical recognizer limits | Preserved; see media/manual smoke evidence | Real microphone/DA recognizer/permissions |
| OCR | Legacy screenclip plus callback/WinRT bridge | Preserved; callback GUID checks and sanitized diagnostics | Full interactive snip, cancel, clipboard and packaged callback tests |
| Analysis/correction | Heuristic C# POS/spelling guesses; real analyzer absent | Neutral rendering and disabled analysis controls; preferences preserved | Core document-analysis bridge and right-click corrections |
| Login/backend | Not implemented | Offline suggestions require no backend/token; account sync unavailable | Backend contract/access and Windows secure credential storage |
| Packaging/update | No installer/updater | Validated self-contained MSIX; trusted signing gate; HTTPS App Installer scripts; beta identity/rollback documented | Signing identity/feed, install/update/tamper/uninstall/rollback on VM |
| Build reproducibility | Only cached local builds proven | Pinned SDK/SQLite checksum, no protected SDK mutation; fresh committed Windows/Core export builds and all six/54 checks pass | Fresh Windows VM/toolchain installation remains separate |

Read `ReleaseAudit.md` for preserve/refactor/restart comparison, `BridgeContract.md`
for pack/wire details, `Release.md` for secure delivery and `ManualSmoke.md` for
test scope. No source or runtime behavior was removed merely for aesthetic reasons.

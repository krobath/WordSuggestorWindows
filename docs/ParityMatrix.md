# Windows evidence-based parity matrix

## Slice 8 General update candidate — 2026-11-12

| Controls | Implementation | Evidence boundary |
|---|---|---|
| G04 installed version | Native package identity/version; development version when loose | Software tests PASS; real signed beta context not installed here |
| G05 automatic checking | Typed WinRT readback, mixed/unknown state; verified setter with restore path | Simulated write/readback tests PASS; production editing deliberately read-only pending trusted packaged qualification |
| G06 manual checking | Canonical bounded HTTPS feed plus OS availability; single-flight/cancel | Synthetic HTTP/native-adapter tests PASS; real public installed feed not qualified |
| Explicit update action | Recheck, native confirmation, existing dirty-document close guard, one-use OS handoff | Physical WPF input/native modal tests PASS; actual App Installer install/relaunch not run |

See [Windows update contract](WindowsUpdates.md) and [slice-8 evidence](qualification/WSA-UX-029_slice8.md).
Existing Windows scheduling is unaffected by the editing gate. Neither fake policy
results nor API presence attest native authority, trust or N→N+1. Hello native
acceptance is deferred by the user as of 2026-11-12. Other human/hardware/live
account and release gates remain open; earlier dated statuses are historical.

## Slice 7 account candidate — 2026-11-11

| Controls | Implementation | Evidence boundary |
|---|---|---|
| P01–P06 | Authoritative identity, login/flag-gated verification-first registration, resend/cooldown | Real HTTP serializer with synthetic backend; native WPF login; no live account mutation |
| P07 | Credential Manager + guarded Hello-encrypted recovery and cancellation | Native Credential Manager PASS; key availability false; positive PIN/biometric test BLOCKED |
| P08–P13 | Canonical ID drafts, explicit two-part save, optimistic conflict/partial status | HTTP fixtures and native UTF-8 profile save; unknown existing IDs retained |
| P14–P18 | Authoritative email/entitlement, capability-gated email change, current-password security action | Synthetic contract tests; no payment/license behavior change |
| P19–P21 | Separate local/all-device logout, secure offline revocation, single-flight/lock/stale guards | Native confirmation Yes/No plus recovery/error fixtures; OS lock simulated, hardware acceptance open |

See [account/session/Hello contract](AccountAndHello.md) and [slice-7 qualification](qualification/WSA-UX-029_slice7.md). This candidate does not close positive native Hello, a dedicated
live-backend account or human accessibility/hardware gates. Older dated statuses
below apply only to their recorded source, not current account behavior.

## Slice 6 runtime parity — 2026-11-11

| Controls | Current implementation | Qualification limit |
|---|---|---|
| T01–T04 | Real Core word classes, semantic/punctuation diagnostics, master/dependencies | Internal editor only; source/pack dependent |
| T05–T06 | Both comma styles and contextual/graph-only Core policies; schema migration | Core fixtures; meaning-dependent native confirmation remains manual follow-up |
| L01–L02 | Separate local capture/sharing preference; local-off also disables share | Sharing adds no upload/training |
| L03–L04 | Saved choice detail/Back/delete-one/confirmed delete-all/reset | Native selection/detail plus state tests; confirmation cancel/confirm manual gate |
| E01–E02 | Existing tracking master and explicit bounded local example opt-in | Content is separate from diagnostics and sharing consent |

The new [analysis/learning evidence](qualification/WSA-UX-029_slice6.md) distinguishes
native automation from human Narrator/audio/DPI signoff. No unknown-word C# guesses,
external diagnostic edits, account or update toggle is enabled by these controls.

## Historical Settings F01–F20 candidate — 2026-11-11

The table below records the slice-5 candidate checkpoint. Its pending native
checks are superseded for the newer source by the 48-check slice-6 suggestion
gate and the general GUI gate in the linked current qualification. Human
Narrator/audio and extra DPI/monitors remain open.

| Controls | Implemented boundary | Evidence / remaining gap |
|---|---|---|
| F01–F04 | Global permission, density, Core spacing, five schedulers | State/real Core transactions and controlled cadence pass; physical UI pending |
| F05–F07 | Three exact, conflict-swapping digit schemes; per-lane registration rollback | Native collision/AltGr/three-lane acceptance pending |
| F08–F11 | Shared cancellable hover TTS, actual word-info/families, English internal period, idle hide | Core eligibility passes; native presentation/interaction pending |
| F12–F16 | Core source preferences with actual pack capability gates | Core source fixtures pass; no missing pack data fabricated |
| F17–F19 | Explicit installed catalog, language-scoped IDs, bounded independent lanes | Core seed used only as explicit isolated test fixture; desktop pending |
| F20 | FlowDocument wrap without document replacement | Implementation complete; native text/caret/undo qualification pending |

F10 canonical-source correction: English newline transitions only; Windows external
period rewriting is disabled. See [full evidence/limits](qualification/WSA-UX-029_slice5.md).

Updated 2026-11-10 (Settings slices 1–4 qualified within recorded scope; later functional slices open). Source: baseline audit at Windows `4d67106`, current source,
real Core contract tests and real-desktop smoke. Historical "Done" labels mean code
was delivered, not all target programs or accessibility modes were verified.

| Capability | Baseline finding | Current implementation/evidence | Remaining qualification |
|---|---|---|---|
| Native shell | WPF/.NET 9 built; 560×68 toolbar observed | WPF/.NET 10, compact non-activating toolbar; desktop startup/focus checks pass | Human visual approval |
| Editor window | Shared topmost toolbar/editor conflicted with new handoff | Separate normal activating editor; actual local edits/Unicode/paragraph/selection checks pass with Notepad open | Extended document/file-dialog checks recorded in delivery note |
| Core reuse | Real CLI but failed builds could return success/stale binary | Versioned stdin JSON, timeout/cancellation/process reap, generation guards; real fixtures and failure-host tests pass | Mac-machine fixture execution |
| Speech Settings | Speed label/rate disagreed; strategy stored but unused; heuristic highlight; orphan candidate audio | Slice 4: schema-3 speed migration, four real typing strategies, native OneCore/SAPI timing and non-selecting adorner, scoped cancellation and shared candidate audio | Human listening/Narrator/mixed DPI; physical Windows 10; platform sentence segmentation differences; see slice-4 matrix |
| Core Settings bridge | v1 offered only suggestions/user words | Slice 3: v1 capability discovery and typed v2 source/domain/morphology/UTF-16 analysis queries; 169 portable and 58 Windows contract checks | UI wiring in slices 5–6; no adaptive/choice persistence; optional pack data absent; Mac executable run and full Swift suite not green/qualified; see slice-3 evidence |
| Danish pack | Legacy schema 0, no pack_meta | Read-only integrity/schema validation; explicit DA legacy profile; real suggestions pass | Metadata-complete modern DA artifact |
| EN/DE | Empty EN file incorrectly considered installed; DE absent | Empty/corrupt/wrong-language/future schema rejected; routing preserved | Actual compatible EN/DE packs |
| Suggestions/paging | Separate overlay, 10/page, 4 pages | Shared accept path; stale/duplicate results rejected; real mouse/Ctrl+1 pass | Broader keyboard/screen-reader coverage |
| Caret/selection | End-of-document requests; paragraph offsets/selection fragile | Context before caret, UTF-16 editing transaction, actual multiline emoji/Danish and selection checks pass | Long-document editing/undo soak |
| Follow/static placement | Mouse/approximate bounds treated as caret; primary-screen clamp | Confirmed caret only, static fallback, persisted physical position and per-monitor geometry | Real mixed DPI/multiple displays; one 96-DPI display tested |
| External Notepad | Optimistic foreground SendInput; mouse used local path | Snapshot-bound native Edit/RichEdit commit, verified resulting text/caret; real mouse/Ctrl+1/clipboard checks pass | >60,000 UTF-16 units unsupported; more Notepad versions |
| Word/Outlook/browser commits | Generic route present without E2E qualification | Unsafe route removed; those targets fail closed for suggestion commits | Isolated qualified adapter per host; no generic injection |
| Selection import | UIA, Word COM, automatic clipboard fallback | UIA/Word COM retained on explicit actions; no passive full-selection poll; explicit snapshot-qualified clipboard import | App-specific import matrix remains mostly untested |
| Settings/data | Local JSON; no backup/future-schema protection | [Slice 2](qualification/WSA-UX-029_slice2.md): same location, schema 2, byte-exact v1 backup, rolling atomic backup, strict invalid/future protection, live authority and concurrent legacy protection | Signed upgrade/uninstall/rollback tests; no cross-process writer introduced |
| Settings UI/content against macOS beta.9 | Horizontal categories; no Profile/Learning categories, speech mixed into General; many controls missing or inactive | Eight categories/help/legacy plus [slice 2](qualification/WSA-UX-029_slice2.md) live save, independent interface/writing language and real caret permission; 63 state/58 Settings desktop checks, actual Notepad overlay checks | Full 72-control functional parity; older non-Settings copy not fully translated; human accessibility/DPI review and positive Hello/OS-update qualification remain open |
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

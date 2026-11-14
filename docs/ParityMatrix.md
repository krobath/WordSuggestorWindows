# Windows evidence-based parity matrix

## G05 Settings page verification (2026-11-14)

The operator reached Installed apps; the old nonce-anywhere check was a false
positive for app details. List pages are now rejected; details need header/spec/
version proof. A guarded in-package launcher replaces the unreliable external URI.
Installed own-package launch now verifies app_details and its update control.
Build/app105/UpdateState38 retain their recorded scope. Release scripts now pass
175 checks, including preserved before/after navigation samples and read failures.
A fresh signed [manual control experiment](qualification/WSA-UX-029_g05_manual_control_roundtrip.md)
also mismatched after the corrected operator sequence: Off was reported by the
operator but On read after reopening. The next diagnostic captures UI/native data
before AND after navigation; its installed run is NOT RUN. No automatic clicks or setters.
G05 remains read-only. See [measured read-only acceptance](qualification/WSA-UX-029_g05_packaged_settings_page.md).


## G05 Settings window correction (2026-11-14)

The probe admitted IntPtr.Zero and assumed Settings owned the top-level window.
Both are corrected; a real hosted Settings window and 769 UIA nodes read without
error on this VM. Release scripts: 151 PASS. The specific app page still needs its
fresh UI-only check; G05's separate 0x800706F7 remains unqualified/read-only.
See [reproduction, live proof and fresh test](qualification/WSA-UX-029_g05_hosted_window_fix.md).


## G05 measured input/schema result (2026-11-14)

The operator completed the isolated run: all 21 input attempts still reject with
0x800706F7, across baseline/capabilities and schemas 2018/2021. Original policy
and cleanup verified. Settings UI remains unqualified; diagnostic labels/error
metadata and a read-only UI-only follow-up are added. G05 stays read-only.
See [measured matrix and next test](qualification/WSA-UX-029_g05_input_schema_acceptance.md).


## G05 input/schema investigation (2026-11-13)

Seven native input controls, same-identity schema comparison and a read-only
Settings probe are implemented. Build/app105/UpdateState35/release135 pass.
The native attempt stopped at UAC BEFORE installation; the new comparisons are
NOT RUN and G05 remains unqualified/read-only. No VM cause is established.
See [exact evidence, research and continuation](qualification/WSA-UX-029_g05_input_schema_diagnosis.md).


## G05 isolated native diagnosis (2026-11-13)

Both installed baseline and capability variant fail at write_policy/0x800706F7;
Windows cmdlet, PowerShell WinRT and native C++ controls fail identically,
including factory/manual no-op and 0/6-hour comparisons. All
settings remain unchanged/restored and temporary trust/install cleaned. Broad
capabilities alone do not solve it, so production rights/template stay unchanged.
New diagnostic-only guards and actual off/on proof requirements are implemented;
build, app105, UpdateState31 and release131 pass. G05 stays read-only. The host is current GA Windows 11 26H2, not an
established Insider/VM fault; native/data diagnosis continues on this same VM.
See [measured diagnosis and exact isolated procedure](qualification/WSA-UX-029_g05_native_diagnosis.md).
Earlier acceptance records retain their scope; no G05/public release PASS implied.


## Measured native installer and GUI acceptance (2026-11-13)

The default combined user run completed: 22/24 native checks and all 56 installed
GUI checks pass. Installer dialog detection/clearance, real editor/Notepad,
manual update, dirty rollback, integrity negatives and uninstall data retention
are verified together. Cleanup independently confirmed. Exit 3 remains correct:
automatic update required dialog handling; G05 still returns 0x800706F7/read-only.
No unchanged rerun is needed now; investigate those two native checks next.
Production signing/HTTPS/public release and remaining human gates stay open;
Hello user-deferred. See [exact evidence and next work](qualification/WSA-UX-029_slice14_installer_dialog_acceptance.md).
Earlier dated sections below retain their historical scope.


## Package integrity follow-up (2026-11-13)

Current Settings and fresh build pass (0 warnings/errors); the reported
OpenInstaller call was already removed by 92f2ed5. New MSIXs enforce Windows
runtime content integrity. Native testing now proves corrupt-signature rejection,
older/same-version behavior and data preservation after uninstall. A payload-only
corrupt source was accepted, but all 441 installed content files matched the
trusted original; registration alone never qualifies integrity. Release tests: 94.
G05 stays blocked; this run deliberately skips GUI/automatic update. Full release
remains unqualified, Hello user-deferred, production signer/feed absent.
See [exact evidence and next steps](qualification/WSA-UX-029_slice14_package_integrity.md).
Earlier dated sections below preserve historical, narrower results.



## WSA-UX-029 slice 15 - final software and acceptance boundary (2026-11-13)

Settings row editors now expose localized UIA names, including after language
changes. Layout automation covers two account states, two locales and all eight
categories. The existing clean-source script supports FullRegression and records
software gates separately from native/human release acceptance. See the
[procedure and remaining matrix](FinalAcceptance.md) and
[measured results](qualification/WSA-UX-029_slice15.md).
Sharing receiver, native installed updates, human accessibility/mixed DPI and
user-deferred Hello remain explicit acceptance gaps; full sprint/release stay open.


## WSA-UX-029 slice 14 - signed local fixtures, native acceptance pending (2026-11-13)

The existing MSIX flow now prepares three isolated signed test packages without
installing or changing device trust. The update harness covers extended data,
higher-version rollback and packaged policy qualification. Build, 56 release
checks, 54 standalone payload GUI checks and synthetic Core/store readback pass.
Installed update/rollback and G05 remain unqualified pending explicit trust
approval; no production signer/feed/publication exists. See [actual evidence](qualification/WSA-UX-029_slice14.md)
and [release procedure](Release.md). macOS/backend/shared sources unchanged.


## WSA-UX-029 slice 13 - sharing client ready, receiver pending (2026-11-13)

The Windows client prepares the unchanged Core handoff, persists an account-bound
idempotent queue and reuses secure account HTTPS transport. Schema 9 adds separate
profile-bound consent; all real sharing stays source-gated/off. The user assigned
the receiver to the macOS backend agent. See [client contract](UnknownWordSharing.md),
[backend prompt](handoffs/WSA-UX-029_slice13_backend_agent_prompt.md) and
[qualification](qualification/WSA-UX-029_slice13.md). A07 remains unavailable;
mock success is not deployed-backend acceptance. Earlier entries are historical.


## WSA-UX-029 slice 12 - verified typing lookup (2026-11-13)

Automatic Danish lookup now works in the native editor and supported Notepad with
separate opt-in, Core-owned word boundaries, rate limits and late-write protection.
A06 is implemented; A07 remains unavailable pending the macOS-owned backend. Schema
8 never promotes manual consent into automatic permission. See [contract](AutomaticUnknownWords.md),
[qualification](qualification/WSA-UX-029_slice12.md) and the approved
[remaining four-slice scope](sprints/WSA-UX-029_closeout_slices12_15.md).
Earlier dated entries below are historical. Hello remains user-deferred; sprint
and release acceptance remain open. No macOS app/library or backend files changed.


## WSA-UX-029 slice 11 — explicit unknown-word import, 2026-11-13

Users can explicitly look up and import one Danish word through Settings > Advanced
or the editor menu, after separate online consent. Core validates dictionary data;
Windows atomically saves the separate local overlay. Imported words work offline
and survive restart; opting out stops lookup, explicit clear removes imported data.
A06 is partial (explicit import only), A07 unavailable; automatic typing capture is
off. Schema 7 defaults new/migrated consent to off. See [contract](UnknownWordImport.md),
[plan](sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md) and
[qualification](qualification/WSA-UX-029_slice11.md). Older dated statements below
are historical. Native Hello remains user-deferred and release gates remain open.


## Slice 9 Advanced and complete status mapping — 2026-11-12

| Controls | Current behavior | Evidence boundary |
|---|---|---|
| A01 | Aggregated bounded timings/counts, no individual key/text log | State/worker tests; no performance SLA |
| A02 | App events independently filtered | Persistent flags and runtime metadata |
| A03 | Confirmed/static/fallback caret state and verified NativeEdit commit route | Actual app/Notepad gate and typed metadata; unsupported external apps remain unsupported |
| A04 | Independent hidden/local/external panel state | New schema-6 flag, shared legacy merge and native UI |
| A05 | Safe bridge outcomes plus negotiated Core operation summary | Real Core contract; raw logger/stderr stays off |
| A06–A07 | Visible disabled unknown-word lookup and sharing | Unavailable CLI capability, no substitution/upload/consent |
| A08 | Existing separate Windows settings | Shared authority; no recursive or Mac neural controls |

All 72 implementation/manual statuses are in
[the current status matrix](qualification/settings-parity-status.v1.json); the old
slice-0 register stays baseline history. See [diagnostics/privacy contract](Diagnostics.md) and [slice-9 qualification](qualification/WSA-UX-029_slice9.md).
No blanket parity or release approval follows. G05 installed mutation, Hello,
live account and human/installed-package acceptance remain separately open.

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
| Editor tabs | Single Windows document; current macOS has multiple tabs | Native TabControl, independent documents/undo/selection, dirty close guards, per-file save, local draft/active-tab recovery; model and actual desktop tests pass | Human Narrator, many-tab layout/high contrast and microphone/playback during switching; undo history is not restored across app restart |
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

## Release tooling status — 2026-11-12

Windows x64 package/manifest/feed validation and private O-052 uploader are
implemented; fixture and standalone payload results are tracked in
[WSA-RT-017 qualification](qualification/WSA-RT-017_release_cleanup.md).
This is not signed-install, update, public download or Mac Sparkle qualification.
Read [Release.md](Release.md) for the exact remaining gates and safe workflow.

## Unknown-word dependency after slice 10 — WSA-UX-029, 2026-11-12

A06/A07 remain unavailable. The new Core/Windows assessment is a prerequisite, not an implemented lookup/sharing UI or full parity. The pinned Core revision now includes only additive CLI code; macOS/library/pack behavior is unchanged. See [slice-10 evidence](qualification/WSA-UX-029_slice10.md).

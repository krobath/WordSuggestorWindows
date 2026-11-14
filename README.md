# WordSuggestorWindows

## G05 Settings page verification (2026-11-14)

The operator reached Installed apps; the old nonce-anywhere check was a false
positive for app details. List pages are now rejected; details need header/spec/
version proof. A guarded in-package launcher replaces the unreliable external URI.
Installed own-package launch now verifies app_details and its update control.
Build/app105/UpdateState38 retain their recorded scope. Release scripts now pass
188 checks, including passive observation without activation, drift and read failures.
The [manual control experiment](docs/qualification/WSA-UX-029_g05_manual_control_roundtrip.md)
now verifies Off/Off BEFORE reopening and On/On AFTER reopening. Native values
were unchanged in both phases, with on-launch/background still true. Cause unknown;
The [passive observation](docs/qualification/WSA-UX-029_g05_passive_observation.md)
completed: four Off/Off samples over 15.74 seconds, then On/On after HUMAN Settings
navigation; native values unchanged throughout. No package reactivation by the observer.
The used command is retired. Cause and effective saved Off policy remain unqualified.
G05 remains read-only. See [measured read-only acceptance](docs/qualification/WSA-UX-029_g05_packaged_settings_page.md).


## G05 Settings window correction (2026-11-14)

The probe admitted IntPtr.Zero and assumed Settings owned the top-level window.
Both are corrected; a real hosted Settings window and 769 UIA nodes read without
error on this VM. Release scripts: 151 PASS. The specific app page still needs its
fresh UI-only check; G05's separate 0x800706F7 remains unqualified/read-only.
See [reproduction, live proof and fresh test](docs/qualification/WSA-UX-029_g05_hosted_window_fix.md).


## G05 measured input/schema result (2026-11-14)

The operator completed the isolated run: all 21 input attempts still reject with
0x800706F7, across baseline/capabilities and schemas 2018/2021. Original policy
and cleanup verified. Settings UI remains unqualified; diagnostic labels/error
metadata and a read-only UI-only follow-up are added. G05 stays read-only.
See [measured matrix and next test](docs/qualification/WSA-UX-029_g05_input_schema_acceptance.md).


## G05 input/schema investigation (2026-11-13)

Seven native input controls, same-identity schema comparison and a read-only
Settings probe are implemented. Build/app105/UpdateState35/release135 pass.
The native attempt stopped at UAC BEFORE installation; the new comparisons are
NOT RUN and G05 remains unqualified/read-only. No VM cause is established.
See [exact evidence, research and continuation](docs/qualification/WSA-UX-029_g05_input_schema_diagnosis.md).


## G05 isolated native diagnosis (2026-11-13)

Both installed baseline and capability variant fail at write_policy/0x800706F7;
Windows cmdlet, PowerShell WinRT and native C++ controls fail identically,
including factory/manual no-op and 0/6-hour comparisons. All
settings remain unchanged/restored and temporary trust/install cleaned. Broad
capabilities alone do not solve it, so production rights/template stay unchanged.
New diagnostic-only guards and actual off/on proof requirements are implemented;
build, app105, UpdateState31 and release131 pass. G05 stays read-only. The host is current GA Windows 11 26H2, not an
established Insider/VM fault; native/data diagnosis continues on this same VM.
See [measured diagnosis and exact isolated procedure](docs/qualification/WSA-UX-029_g05_native_diagnosis.md).
Earlier acceptance records retain their scope; no G05/public release PASS implied.


## Measured native installer and GUI acceptance (2026-11-13)

The default combined user run completed: 22/24 native checks and all 56 installed
GUI checks pass. Installer dialog detection/clearance, real editor/Notepad,
manual update, dirty rollback, integrity negatives and uninstall data retention
are verified together. Cleanup independently confirmed. Exit 3 remains correct:
automatic update required dialog handling; G05 still returns 0x800706F7/read-only.
No unchanged rerun is needed now; investigate those two native checks next.
Production signing/HTTPS/public release and remaining human gates stay open;
Hello user-deferred. See [exact evidence and next work](docs/qualification/WSA-UX-029_slice14_installer_dialog_acceptance.md).
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
See [exact evidence and next steps](docs/qualification/WSA-UX-029_slice14_package_integrity.md).
Earlier dated sections below preserve historical, narrower results.



## WSA-UX-029 slice 15 - final software and acceptance boundary (2026-11-13)

Settings row editors now expose localized UIA names, including after language
changes. Layout automation covers two account states, two locales and all eight
categories. The existing clean-source script supports FullRegression and records
software gates separately from native/human release acceptance. See the
[procedure and remaining matrix](docs/FinalAcceptance.md) and
[measured results](docs/qualification/WSA-UX-029_slice15.md).
Sharing receiver, native installed updates, human accessibility/mixed DPI and
user-deferred Hello remain explicit acceptance gaps; full sprint/release stay open.


## WSA-UX-029 slice 14 - signed local fixtures, native acceptance pending (2026-11-13)

The existing MSIX flow now prepares three isolated signed test packages without
installing or changing device trust. The update harness covers extended data,
higher-version rollback and packaged policy qualification. Build, 56 release
checks, 54 standalone payload GUI checks and synthetic Core/store readback pass.
Installed update/rollback and G05 remain unqualified pending explicit trust
approval; no production signer/feed/publication exists. See [actual evidence](docs/qualification/WSA-UX-029_slice14.md)
and [release procedure](docs/Release.md). macOS/backend/shared sources unchanged.


## WSA-UX-029 slice 13 - sharing client ready, receiver pending (2026-11-13)

The Windows client prepares the unchanged Core handoff, persists an account-bound
idempotent queue and reuses secure account HTTPS transport. Schema 9 adds separate
profile-bound consent; all real sharing stays source-gated/off. The user assigned
the receiver to the macOS backend agent. See [client contract](docs/UnknownWordSharing.md),
[backend prompt](docs/handoffs/WSA-UX-029_slice13_backend_agent_prompt.md) and
[qualification](docs/qualification/WSA-UX-029_slice13.md). A07 remains unavailable;
mock success is not deployed-backend acceptance. Earlier entries are historical.


## WSA-UX-029 slice 12 - verified typing lookup (2026-11-13)

Automatic Danish lookup now works in the native editor and supported Notepad with
separate opt-in, Core-owned word boundaries, rate limits and late-write protection.
A06 is implemented; A07 remains unavailable pending the macOS-owned backend. Schema
8 never promotes manual consent into automatic permission. See [contract](docs/AutomaticUnknownWords.md),
[qualification](docs/qualification/WSA-UX-029_slice12.md) and the approved
[remaining four-slice scope](docs/sprints/WSA-UX-029_closeout_slices12_15.md).
Earlier dated entries below are historical. Hello remains user-deferred; sprint
and release acceptance remain open. No macOS app/library or backend files changed.


## WSA-UX-029 slice 11 — explicit unknown-word import, 2026-11-13

Users can explicitly look up and import one Danish word through Settings > Advanced
or the editor menu, after separate online consent. Core validates dictionary data;
Windows atomically saves the separate local overlay. Imported words work offline
and survive restart; opting out stops lookup, explicit clear removes imported data.
A06 is partial (explicit import only), A07 unavailable; automatic typing capture is
off. Schema 7 defaults new/migrated consent to off. See [contract](docs/UnknownWordImport.md),
[plan](docs/sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md) and
[qualification](docs/qualification/WSA-UX-029_slice11.md). Older dated statements below
are historical. Native Hello remains user-deferred and release gates remain open.


## Settings slice 9 diagnostics candidate — 2026-11-12

Advanced now has five effective, independent diagnostic switches, including panel
state, plus the two explicitly unavailable unknown-word controls and the existing
Windows legacy entry. Metadata uses a bounded queue/files and aggregated timings;
there is no automatic upload, document text or raw Core error logging. Core is
pinned to `56aba63042796b2a6c398828c3737ab0cb02eae5` with opt-in structured summaries.
Schema 6 adds panel-state preference with exact old-format backup on first write.
See [runtime/privacy contract](docs/Diagnostics.md),
[all 72 control statuses and test/manual evidence](docs/qualification/WSA-UX-029_slice9.md),
and [change note/file scope](docs/changes/2026-11-12_wsa_ux_029_slice9_advanced_diagnostics.md).

From the WordSuggestorWindows repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode AdvancedState -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode AdvancedUi -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
```

Desktop gates need an active unlocked Windows session. HelloNative stays deferred
by user request and is not in All. Production update-policy editing, unknown-word
lookup/sharing, real trusted MSIX update and human accessibility acceptance remain
open. Do not interpret implementation/test candidates as official beta qualification.
Existing tabs/restart/release working-tree files/hunks remain outside this slice.
Final isolated-source gates PASS: build, Core 354 + 6 DA fixtures, app 67,
Advanced 34, complete Settings regression, editor/Notepad GUI 37, Media 3 and
safety/mapping 41. Tested display: 96 DPI, one monitor. Native installed update
and human acceptance remain open. Earlier dated entries below are historical checkpoints.

## Settings slice 8 update candidate — 2026-11-12

General now shows Windows' actual App Installer scheduling state and separates
checking for updates from explicitly opening the installer. Manual handoff requires
valid HTTPS metadata and OS agreement, confirmation, then normal application close
with the existing unsaved-document guard. Earlier native UI checks passed; the final isolated desktop rerun is blocked by
a disconnected session, and Media fails on the same black fixture as the prior
baseline. State/contract gates pass; this is not an all-green release.
Native policy editing remains read-only
until an installed trusted test package passes change/read/restore qualification.
This does not disable automatic updates already scheduled by Windows.
Windows Hello hardware acceptance is deferred by the user as of 2026-11-12.
See [update contract](docs/WindowsUpdates.md),
[evidence/manual matrix](docs/qualification/WSA-UX-029_slice8.md) and
[change note/file scope](docs/changes/2026-11-12_wsa_ux_029_slice8_app_installer_updates.md).

Run from the WordSuggestorWindows repository directory:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode UpdateState -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode UpdateUi -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
```

Both projects use .NET 10 WPF with managed Windows SDK 10.0.22621.57, pinned in
the project files and dependency lock. The compile target is
`net10.0-windows10.0.22621.0`; the Windows manifest/API baseline remains 19041.
Legacy `bin/Debug/net10.0-windows` output paths remain stable for existing scripts;
custom `--artifacts-path` layouts still work. Positive policy mutation requires
Windows 11 and remains unqualified; Windows 10 behavior is not tested here.
Older dated Settings entries below are historical checkpoints.

## Settings slice 7 account candidate — 2026-11-11

The Profile category now has backend-backed login/verification-first registration,
draft profile save, password/email changes, entitlements and separate logout flows.
Recovery uses Credential Manager and a guarded native Hello adapter. No account is
created automatically; local suggestions remain offline. Native Hello is unavailable
on the current host and is **not positively qualified**. See the
[account/privacy/recovery contract](docs/AccountAndHello.md),
[test evidence](docs/qualification/WSA-UX-029_slice7.md) and
[change note/file scope](docs/changes/2026-11-11_wsa_ux_029_slice7_account_hello.md).

```powershell
powershell -ExecutionPolicy Bypass -File scripts	est_settings_app.ps1 -Mode AccountState
powershell -ExecutionPolicy Bypass -File scripts	est_settings_app.ps1 -Mode AccountUi -SkipBuild
# Explicit native PIN/biometric test, excluded from unattended All; exit 3 = blocked:
powershell -ExecutionPolicy Bypass -File scripts	est_settings_app.ps1 -Mode HelloNative -SkipBuild
```

Existing tabs/restart/release working-tree changes remain outside this slice.
Earlier dated Settings statuses below are historical and superseded within scope.

## Settings slice 6 — 2026-11-11

The internal editor now shows Core word classes and diagnostic corrections.
Right-click to review a correction/interpretation; grouped corrections preserve
native undo. Learning offers separate local opt-in, saved choices, deletion and
reset. Choices are not user words and are not uploaded or used for ranking.
See [runtime/data contract](docs/AnalysisAndLearning.md),
[qualification](docs/qualification/WSA-UX-029_slice6.md) and
[changed files](docs/changes/2026-11-11_wsa_ux_029_slice6_analysis_learning.md).

Run `scripts/test_settings_app.ps1 -Mode AnalysisState` without desktop input;
`-Mode AnalysisUi` or `-Mode All` requires an unlocked desktop and uses synthetic
mouse/keyboard input. Existing Core/app/GUI/media gates remain. No secrets needed.

## Settings slice 5 — 2026-11-11

Suggestion preferences now have implementation paths for density, Core insertion
plans/source options, three lanes, hover/fade and editor wrapping. Native desktop
qualification is **pending**, not a release signoff. See [contract](docs/Suggestions.md)
and [results/remaining smoke](docs/qualification/WSA-UX-029_slice5.md).

Build with the existing `scripts/build_app.ps1`; run
`powershell -ExecutionPolicy Bypass -File scripts/test_settings_app.ps1 -Mode SuggestionState`
for noninteractive checks. On a connected, unlocked desktop run the same script
with `-Mode All`; this includes the new physical-input/Notepad fixture. Existing
Core, app, GUI and media gates remain required. No production secrets needed.

Native Windows WPF host for WordSuggestorCore. Current delivery: functioning beta
implementation and signing-ready x64 MSIX payload; public release qualification is
still open. See [release evidence](docs/changes/2026-11-08_wsa_rt_016_windows_beta_hardening.md).

## Prerequisites

- Windows 10 2004+ / Windows 11, x64, ordinary desktop user session.
- .NET SDK 10.0.403 (pinned in `global.json`). A local SDK under
  `.artifacts/dotnet` is automatically discovered; otherwise install the matching SDK.
- Swift 6.3.0 Windows toolchain/runtime, Visual Studio 2022 C++ tools and Windows
  SDK 10.0.22621.0. Windows SDK MakeAppx/SignTool are needed for packaging.
- Adjacent `WordSuggestorCore` checkout, including the tracked Danish
  `Ressources/da_lexicon.sqlite`. See `dependencies.lock.json` for qualified inputs.
  Other sibling repos are references, not runtime requirements. No production secrets.

.NET 10 LTS replaces .NET 9, whose support ends on 2026-11-10 according to
[Microsoft's support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

## Build, test, run

From the workspace parent (containing both repos):

```powershell
powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\test_core_cli.ps1
powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\test_app.ps1
powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild
```

To rebuild and restart an already running development app safely:

```powershell
powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBootstrap -Restart
```

`-Restart` requests normal app shutdown and waits for save prompts to finish before
building. It never kills the process. If shutdown is cancelled or still pending
after two minutes, the script stops without building. Without `-Restart`, a locked
build output is detected before bootstrap/MSBuild. Closing only the editor leaves
the floating toolbar/app running; use **Afslut WordSuggestor** to exit manually.

Bootstrap downloads checksum-pinned SQLite 3.46.0 when absent, compiles it locally,
and builds Core. Swift 6.3 SDK-module discovery is corrected using local virtual
module maps; no Program Files files are patched. All native exit codes are checked.
Network access is needed for first dependency restore/download, not for suggestions.

The app starts as a non-activating toolbar. Open the separate editor using its
right-hand button. Type an unfinished Danish word. Accept with Ctrl+1…Ctrl+0, Tab
or a candidate click; Ctrl+Left/Right pages candidates. Use Ctrl+O/Ctrl+S for UTF-8
text files. The editor supports multiple native document tabs: Ctrl+T (or Ctrl+N)
creates a tab, Ctrl+Tab/Ctrl+Shift+Tab switches tabs, Ctrl+W closes the current tab,
and Ctrl+Shift+S saves it under a different name. The + action and each tab's close
button provide the same mouse paths. Unsaved tabs show an asterisk and ask before
closing. Opening an already open file activates its existing buffer.
Closing the editor preserves all tabs while the toolbar remains. Tabs, active
document, caret and unsaved drafts are restored on restart; undo history lasts
for the current app session. Draft text is stored locally in
`%LOCALAPPDATA%\WordSuggestor\editor\workspace-v1.json` (with a recovery backup),
separately from diagnostics; it is not uploaded. See the
[tab delivery note](docs/changes/2026-11-09_wsa_ux_013_editor_tabs.md).
Use Filer > Afslut or the toolbar context menu to exit; unsaved changes are guarded.
Reopening the app signals the existing instance instead of creating another toolbar.

From the Windows repo, in an unlocked desktop session:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1
powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_package.ps1 -AppDirectory <artifact-app-directory>
powershell -ExecutionPolicy Bypass -File scripts\test_clean_checkout.ps1
```

GUI smoke uses its own synthetic Notepad document and isolated settings. It moves
focus/mouse during the test. Close other WordSuggestor instances first. Tests use
`WORDSUGGESTOR_DATA_ROOT` for isolation; normal use keeps existing LocalAppData.
Clean-checkout smoke exports committed Windows/Core revisions without build output.

To test only the editor while another WordSuggestor instance stays open, use
`scripts\test_gui.ps1 -EditorOnly`. This uses a separate data profile and skips
external Notepad/global-hotkey checks. Human screen-reader and mixed-DPI checks
remain separate.

An independent development build can be created without overwriting an open app:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1 -ArtifactsPath .artifacts\editor-tabs
powershell -ExecutionPolicy Bypass -File scripts\run_app.ps1 -SkipBuild -SkipBootstrap -AppPath .artifacts\editor-tabs\bin\WordSuggestorWindows.App\debug\WordSuggestorWindows.App.exe
```

Exit the old normal instance through **Afslut WordSuggestor**, or add `-Restart`
to the launch command before trying the new build with your normal data.
`test_app.ps1 -RunnerPath <smoke-exe>` and `test_gui.ps1 -RunnerPath <smoke-exe>
-AppPath <app-exe> -FrameworkDependentApp` can exercise this independent build.

For a built Core, `test_core_cli.ps1 -SkipBootstrap` and
`run_app.ps1 -SkipBuild -SkipBootstrap` avoid rebuilding. `WORDSUGGESTOR_SUGGEST_CLI_PATH`
is an explicit developer override. Installed payloads resolve bundled `Core`/`Packs`
without source repos or SDKs. Missing Core/packs produce a calm status, not startup failure.

## Current limits and release

External editing is qualified for Notepad native text controls, up to 60,000 UTF-16
units. Word/Outlook/browser commits are disabled pending target-specific qualification;
UIA/Word-COM selection import remains separate. Clipboard copy is an explicit import
menu action with verification, not an automatic commit fallback. Unknown caret geometry
uses static placement. Real mixed-DPI/multi-monitor and human screen-reader tests remain open.

Core-backed internal document analysis, reviewed corrections and domain lanes are
implemented. Account/backend sync remains a later slice. Unknown words are not
classified by C# guesses; missing pack/capability states stay explicit. Existing TTS, STT, OCR, settings and
insights adapters remain; see the smoke matrix for the scope actually exercised.

MSIX packaging bundles .NET, Swift, Core and Danish data. Unsigned artifacts are clearly
marked; no update metadata is generated until trusted signing succeeds. No signing
certificate/feed is currently configured. [Release.md](docs/Release.md) covers signing,
HTTPS beta updates, data retention, rollback and remaining installation gates.

## Settings development

Open **Indstillinger** on the floating toolbar. The Windows-native sidebar now
contains Profile and seven categories; **Avanceret → Åbn legacy-indstillinger**
opens the previous Windows settings. Ordinary preferences save immediately;
**Luk** closes the primary window. The legacy window retains **Gem/Annuller**.
Interface language (Follow Windows/Danish/English) is independent of writing
language. The Settings and legacy surfaces are translated; older app surfaces
still contain Danish copy. Disabling Follow caret permission forces static
placement; enabling permission does not itself choose Follow caret.

Schema 5 keeps the historical `settings/settings-v1.json` location. Migration
preserves exact once-only `.v1.bak`–`.v4.bak` originals (as applicable) and the rolling `.bak`. Future/corrupt data
is retained and blocks writes; failed changes are not applied. See
[data recovery and qualification](docs/qualification/WSA-UX-029_slice2.md).
Full release parity and account/Hello remain later work; analysis/local choices
are described in the current slice-6 contract.

From this repository, run the new state and interactive-window tests:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1
```

Use `-Mode State` for noninteractive storage tests; `-Mode Ui` requires an unlocked
desktop and moves the mouse/keyboard with synthetic test data. Do not interact
while it runs. `-SkipBuild -RunnerPath <smoke-exe>` tests isolated build output.
See [slice 2 results and remaining manual gates](docs/qualification/WSA-UX-029_slice2.md).

Slice 3 adds a typed, capability-negotiated Core adapter for source preferences,
explicit domain catalogs, word information and UTF-16 diagnostics. Existing app
suggestions continue through JSON v1; later slices connect the new controls.
Missing pack data and learning persistence are not simulated. See the
[bridge contract](docs/BridgeContract.md) and [slice 3 evidence](docs/qualification/WSA-UX-029_slice3.md).

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode Core
```

The default Settings test now also runs the Core adapter gate. `-Mode Core` needs
the real Core CLI/DA pack, but no account or production secret. The portable gate
requires Python 3.10+ (standard library only). No new UI controls are enabled here.

Slice 4 connects **Speech**: 50–150% tempo in 5% steps, automatic word/sentence/
paragraph reading and native-timed highlighting that leaves the selection/caret
alone. Automatic reading uses confirmed editor/qualified Notepad snapshots; pasted
blocks do not trigger. Native SAPI and OneCore remain offline. Candidate audio now
uses the same owned, cancellable service and selected voice/tempo.

Changing primary voice/tempo explicitly selects app control; the previous system
tempo setting and speech-language option remain in legacy settings. Schema 3
migrates the old effective OneCore tempo once, to the nearest 5% step, and maps
legacy page/all strategies to paragraph. See [speech/data recovery](docs/Speech.md).

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode Speech
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode SpeechUi
```

The default Settings gate includes these tests. Speech plays synthetic audio;
SpeechUi also moves keyboard/focus and opens its own Notepad document. Use an
unlocked desktop and do not interact during the gate. Human listening and physical
accessibility/DPI sign-off are separate. [Slice 4 qualification](docs/qualification/WSA-UX-029_slice4.md)
records exact source scope and all remaining gates.

## Documentation

- [Windows documentation index](docs/README.md)
- [Windows Settings sprint WSA-UX-029 (slices 1–4 delivered)](docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md)
- [Settings slice 0 qualification and actual platform limits](docs/qualification/WSA-UX-029_slice0.md)
- [Settings reference/function analysis](docs/analysis/2026-11-09_settings_parity_analysis.md)
- [Documentation ownership across platforms](docs/DocumentationOrganization.md)
- [Repo audit and architecture decision](docs/ReleaseAudit.md)
- [Architecture and data/focus/commit ownership](docs/Architecture.md)
- [Core JSON and language-pack contract](docs/BridgeContract.md)
- [Plan](docs/Plan.md), [parity matrix](docs/ParityMatrix.md), [UI parity](docs/UiParityPlan.md)
- [Desktop/manual smoke](docs/ManualSmoke.md), [selection compatibility](docs/SelectionImportCompatibilityMatrix.md)
- [Changes](docs/changes/)

## Windows beta release tools — WSA-RT-017

The existing `package_app.ps1` now creates the O-052 three-file release bundle
after trusted signing. `publish_beta_release.ps1` validates, uploads to private
UUID staging, registers pending or publishes a qualified active candidate through
the existing server publisher. Start with `-WhatIf` and the
[Windows release runbook](docs/Release.md); every real operation requires approved
signing/trust and pinned SSH configuration. No secrets belong in the repository.
`test_release_scripts.ps1` exercises fixtures/mocked transport plus the immutable
backend validator. Python 3 and the pinned backend reference are required for that
contract gate. `test_clean_checkout.ps1 -IncludeWorkingTree` additionally supports
a source-only uncommitted snapshot; its default still tests committed revisions.
`test_msix.ps1` does not mutate trust by default: temporary LocalTest trust requires
explicit `-AllowTemporaryMachineTrust` in a disposable test environment.
See [actual results and open native/public gates](docs/qualification/WSA-RT-017_release_cleanup.md).

## Completed-word adapter foundation — WSA-UX-029 slice 10

The pinned Core provides a negotiated read-only assessment of one completed token.
It reuses the shared observer; it does not capture typing, learn, contact a
dictionary or enable unknown-word lookup/sharing. Run
`scripts/test_settings_app.ps1 -Mode CommittedWord` or the existing All suite.
[Contract and boundaries](docs/CommittedWordAssessment.md),
[source/test evidence](docs/qualification/WSA-UX-029_slice10.md).

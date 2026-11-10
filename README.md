# WordSuggestorWindows

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

Bootstrap downloads checksum-pinned SQLite 3.46.0 when absent, compiles it locally,
and builds Core. Swift 6.3 SDK-module discovery is corrected using local virtual
module maps; no Program Files files are patched. All native exit codes are checked.
Network access is needed for first dependency restore/download, not for suggestions.

The app starts as a non-activating toolbar. Open the separate editor using its
right-hand button. Type an unfinished Danish word. Accept with Ctrl+1…Ctrl+0, Tab
or a candidate click; Ctrl+Left/Right pages candidates. Use Ctrl+O/Ctrl+S for UTF-8
text files. Closing the editor preserves the buffer while the toolbar remains.
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

Core-backed document analysis, correction popovers, domain lists and account/backend
sync are not yet implemented. Local C# word-class guesses were removed; corresponding
preferences are retained and controls disabled. Existing TTS, STT, OCR, settings and
insights adapters remain; see the smoke matrix for the scope actually exercised.

MSIX packaging bundles .NET, Swift, Core and Danish data. Unsigned artifacts are clearly
marked; no update metadata is generated until trusted signing succeeds. No signing
certificate/feed is currently configured. [Release.md](docs/Release.md) covers signing,
HTTPS beta updates, data retention, rollback and remaining installation gates.

## Settings development

Open **Indstillinger** on the floating toolbar. The Windows-native sidebar now
contains Profile and seven categories; **Avanceret → Åbn legacy-indstillinger**
opens the previous Windows settings. Use **Gem** to apply changes or **Annuller**
to discard them. Full functional parity and live saving are later sprint slices;
unavailable account/Hello/analysis/learning functions are identified explicitly.

From this repository, run the new state and interactive-window tests:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1
```

Use `-Mode State` for noninteractive storage tests; `-Mode Ui` requires an unlocked
desktop and moves the mouse/keyboard with synthetic test data. Do not interact
while it runs. `-SkipBuild -RunnerPath <smoke-exe>` tests isolated build output.
See [slice 1 results and remaining manual gates](docs/qualification/WSA-UX-029_slice1.md).

## Documentation

- [Windows documentation index](docs/README.md)
- [Windows Settings sprint WSA-UX-029 (slice 1 shell delivered)](docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md)
- [Settings slice 0 qualification and actual platform limits](docs/qualification/WSA-UX-029_slice0.md)
- [Settings reference/function analysis](docs/analysis/2026-11-09_settings_parity_analysis.md)
- [Documentation ownership across platforms](docs/DocumentationOrganization.md)
- [Repo audit and architecture decision](docs/ReleaseAudit.md)
- [Architecture and data/focus/commit ownership](docs/Architecture.md)
- [Core JSON and language-pack contract](docs/BridgeContract.md)
- [Plan](docs/Plan.md), [parity matrix](docs/ParityMatrix.md), [UI parity](docs/UiParityPlan.md)
- [Desktop/manual smoke](docs/ManualSmoke.md), [selection compatibility](docs/SelectionImportCompatibilityMatrix.md)
- [Changes](docs/changes/)

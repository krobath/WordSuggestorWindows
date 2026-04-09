# Windows Smoke And Regression Baseline

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `done`
- Sprint ID: `WSA-TS-001_windows_smoke_and_regression_baseline`

## Summary

Added a canonical smoke-validation flow for the current Windows baseline so the app can be built, launched, and manually verified against a live `WordSuggestorCore` CLI bridge with fewer setup steps.

## User-Visible Behavior

- The Windows app can now be launched through a single script that prepares the bridge and starts the app with smoke-ready startup text.
- The app can open directly into a state where the suggestion list should populate without additional typing.

## Implementation Details

- Added `scripts/run_app.ps1` to:
  - bootstrap `WordSuggestorCore` CLI prerequisites,
  - build the WPF app,
  - launch the app executable with startup text injected through an environment variable.
- Updated the WPF startup path to create `MainWindow` programmatically so startup text can be passed into the first view model.
- Updated `MainWindowViewModel` to support initial editor text and immediate suggestion refresh on startup.
- Added `docs/ManualSmoke.md` as the canonical smoke runbook for this repository.
- Updated the plan, readme, and parity matrix to reflect the new smoke baseline and current Windows feature status.

## Files Changed

- `src/WordSuggestorWindows.App/App.xaml` - removed static startup URI so startup can be configured programmatically
- `src/WordSuggestorWindows.App/App.xaml.cs` - added startup text resolution and manual window creation
- `src/WordSuggestorWindows.App/MainWindow.xaml.cs` - added constructor injection and editor focus on load
- `src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` - added startup text initialization and automatic first refresh
- `scripts/run_app.ps1` - added canonical smoke launch script
- `docs/ManualSmoke.md` - added manual smoke checklist and failure triage
- `README.md` - documented the new smoke assets
- `docs/Plan.md` - marked `WSA-TS-001` done and linked it to `WSA-RT-001`
- `docs/ParityMatrix.md` - updated internal-editor and in-app accept status
- `docs/changes/2026-04-09_wsa_ts_001_windows_smoke_and_regression_baseline.md` - this note

## Feature Flags / Settings

- Optional environment variable:
  - `WORDSUGGESTOR_WINDOWS_STARTUP_TEXT` sets the initial editor text for app startup.
- Optional launch argument:
  - `--sample-text "<text>"` sets the initial editor text when launching the app directly.

## Logging / Telemetry

- No telemetry added.
- Startup and bridge failures still surface through the in-app status text.

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild`
- Result:
  - WPF app build: `PASS`
  - Core CLI diagnostic: `PASS`
  - App launch script: `PASS` (process started and remained responsive)
- Interactive GUI observation remains a manual operator step.

## Known Limitations / Follow-up

- This sprint defines and streamlines the smoke path; it does not add external-app integration.
- `WSA-RT-001` still needs an operator-observed UI smoke to close fully.

# WSA-TS-002_windows_selection_import_app_compatibility_matrix

## Summary

Added a lightweight diagnostic and documentation layer for selected-text import so we can determine empirically which Windows applications support UI Automation selection, which require clipboard fallback, and which block both routes.

## User-visible behavior

- No visible UI workflow changed.
- The `TXT` import behavior remains:
  - internal editor selection,
  - live external UI Automation selection,
  - recent cached UI Automation selection,
  - guarded clipboard fallback.
- WordSuggestor now emits non-content selection-import diagnostic lines prefixed with `WordSuggestor selection import:` to debugger output and `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log`.

## Implementation details

- Added a `SelectionImportDiagnostic` model for timestamped stage/outcome/detail records.
- Added diagnostic emissions in `WindowsSelectionImportService` for:
  - skipped UIA reads,
  - UIA success,
  - recent cached UIA selection success,
  - UIA no-selection result,
  - missing fallback target,
  - sentinel setup failure,
  - foreground activation result,
  - synthetic `Ctrl+C` dispatch,
  - clipboard no-selection result,
  - clipboard fallback success,
  - clipboard restoration success/failure.
- Routed diagnostics from `MainWindow` to `Debug.WriteLine` and `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log`.
- Created `docs/SelectionImportCompatibilityMatrix.md` with app priorities, result codes, and a manual recording template.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/SelectionImportDiagnostic.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/docs/SelectionImportCompatibilityMatrix.md`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_ts_002_windows_selection_import_app_compatibility_matrix.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

Local diagnostics were added via `Debug.WriteLine` and `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log`. No telemetry was added, and selected text content is not logged.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- Application event log check after launch -> no recent `WordSuggestorWindows.App` crash event found
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- The compatibility matrix starts with `UNTESTED` rows; the rows should be updated after manual testing in Word, Outlook, Edge/Chrome, Google Docs, Notepad, PDF viewers, and elevated app scenarios.
- Clipboard fallback remains best-effort when apps block synthetic copy, foreground activation, or text clipboard formats.
- The first build validation was blocked by a still-running local `WordSuggestorWindows.App.exe`; after stopping that test process, the build passed.

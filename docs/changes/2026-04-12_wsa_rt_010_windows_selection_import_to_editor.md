# WSA-RT-010_windows_selection_import_to_editor

## Summary

Implemented the first Windows `TXT` toolbar import path so selected text can be staged into the internal editor instead of only showing placeholder status text.

## User-visible behavior

- Clicking `TXT` now imports selected text from the internal editor when the user has an active RichTextBox selection.
- When no internal editor selection exists, WordSuggestor uses a recent Windows UI Automation-compatible external selection captured while another app was foreground.
- Imported text replaces the internal editor contents, expands the editor if needed, moves the caret to the end of the imported text, and lets the existing suggestion refresh flow run when appropriate.
- If no suitable selection is available, the status message tells the user that no marked text could be found.

## Implementation details

- Added `SelectionImportResult` for imported text, source label, and capture timestamp.
- Added `WindowsSelectionImportService` for Windows UI Automation `TextPattern` selection extraction from the foreground app.
- Added a lightweight `DispatcherTimer` in `MainWindow` that caches external UI Automation selections while WordSuggestor is not the foreground window.
- Updated `ToolbarAction_OnClick` so the `import` action calls the new import path instead of the placeholder view-model status branch.
- Added internal RichTextBox selection extraction through `TextRange(EditorTextBox.Selection.Start, EditorTextBox.Selection.End)`.
- Added `MainWindowViewModel.ImportTextIntoEditor(...)` to normalize imported text, expand the editor, replace the editor buffer, and put the caret at the end.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/SelectionImportResult.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_010_windows_selection_import_to_editor.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added. The status message now reports whether text was imported or whether no selection could be found.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- Windows Application event log check after launch -> no recent `WordSuggestorWindows.App` crash event found

## Known limitations / follow-up

- External selection support depends on the target app exposing selected text through Windows UI Automation `TextPattern`.
- This sprint intentionally does not send synthetic `Ctrl+C` to external apps, because that follow-up needs explicit clipboard preservation and focus guardrails.
- Full analyzer parity after import still depends on the later lexicon-backed analyzer work; this sprint uses the existing Windows editor refresh/coloring baseline after import.

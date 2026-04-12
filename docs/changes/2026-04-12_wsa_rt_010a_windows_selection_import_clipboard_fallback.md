# WSA-RT-010A_windows_selection_import_clipboard_fallback

## Summary

Added a guarded clipboard fallback to the Windows selected-text import flow so the `TXT` toolbar action can work in more external applications than the UI Automation-only baseline.

## User-visible behavior

- `TXT` still prefers selected text inside the internal editor.
- If there is no internal selection, WordSuggestor still tries live or recent cached Windows UI Automation selection.
- If UI Automation does not provide selected text, WordSuggestor now attempts a guarded `Ctrl+C` copy against the most recent external foreground window.
- If the target app accepts the copy shortcut, the copied text is imported into the internal editor and WordSuggestor focus is restored.
- If the copy attempt fails or produces no text, the app avoids importing stale clipboard contents.

## Implementation details

- Extended `WindowsSelectionImportService` with a sentinel-based clipboard fallback:
  - save the current clipboard data object,
  - place a unique sentinel text on the clipboard,
  - bring the most recent external foreground window forward,
  - send `Ctrl+C` via `SendInput`,
  - import the clipboard text only if it changed away from the sentinel and contains text,
  - restore the previous clipboard data object where possible,
  - restore WordSuggestor as foreground window.
- Extended `MainWindow` to remember the most recent non-WordSuggestor foreground window while polling selection state.
- Updated the `TXT` toolbar import order to add clipboard fallback after internal selection, live UIA selection, and recent cached UIA selection.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_010a_windows_selection_import_clipboard_fallback.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added. User-facing import status continues to be reported through the existing status message path.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- Windows Application event log check after launch -> no recent `WordSuggestorWindows.App` crash event found

## Known limitations / follow-up

- Clipboard fallback is best-effort because some applications block synthetic copy, reject programmatic foreground activation, or expose delayed clipboard formats.
- Clipboard restoration is best-effort; Windows clipboard access can fail transiently if another process owns the clipboard.
- Manual smoke should confirm behavior in the priority external applications after this baseline.

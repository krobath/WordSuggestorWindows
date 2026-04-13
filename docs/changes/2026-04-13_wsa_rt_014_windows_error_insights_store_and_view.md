# WSA-RT-014_windows_error_insights_store_and_view

## Summary

Added the first Windows-native error insights baseline backed by local-only tracking data.

## User-visible behavior

- Clicking `INS` opens a native Windows Insights window.
- The view shows:
  - accepted suggestion count,
  - backspace count,
  - sentence boundary count,
  - last-seven-days event count,
  - suggestion-kind breakdown,
  - part-of-speech breakdown,
  - frequent typed-to-accepted corrections,
  - recent local insight events.
- Accepted suggestions are recorded when the user accepts from the internal editor through the existing suggestion paths.
- Backspace and sentence boundary events are recorded from internal editor key input.

## Implementation details

- Added a local JSONL store at `%LOCALAPPDATA%\WordSuggestor\insights\error-insights.jsonl`.
- Added the Windows insights event and snapshot models:
  - `ErrorInsightEvent`
  - `ErrorInsightSummaryRow`
  - `ErrorInsightCorrectionRow`
  - `ErrorInsightsSnapshot`
- Added `WindowsErrorInsightsStore` to append local events and compute aggregate snapshots.
- Wired accepted-suggestion tracking into `MainWindowViewModel` without changing the suggestion engine contract.
- Wired backspace and sentence-boundary tracking into the internal editor key path in `MainWindow`.
- Added `InsightsWindow` as the first WPF summary surface for the toolbar `INS` action.
- Kept `WordSuggestorCore` and the macOS app untouched.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/App.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/InsightsWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/InsightsWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/ErrorInsightCorrectionRow.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/ErrorInsightEvent.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/ErrorInsightSummaryRow.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/ErrorInsightsSnapshot.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsErrorInsightsStore.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/changes/2026-04-13_wsa_rt_014_windows_error_insights_store_and_view.md`

## Feature flags / settings impact

No feature flags or user settings were added.

## Logging / telemetry impact

No telemetry was added. Insights are stored locally under the current Windows user profile.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process was stopped after smoke)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorWindows diff --check` -> `PASS` (only Git CRLF/LF normalization warning)
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- The Windows baseline uses JSONL rather than SQLite. This keeps the implementation simple and local-only while leaving room for a later shared cross-platform insights store.
- Full macOS parity for richer charts/timelines remains a follow-up refinement.
- Morphology/part-of-speech breakdown is based on metadata returned with accepted suggestions, not a full analyzer-backed editor scan.
- Manual verification is still needed to confirm the `INS` window updates after interactive accepted-suggestion, backspace, and sentence-boundary input.

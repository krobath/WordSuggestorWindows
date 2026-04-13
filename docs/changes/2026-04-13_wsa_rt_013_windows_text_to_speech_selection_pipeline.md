# WSA-RT-013_windows_text_to_speech_selection_pipeline

## Summary

Added the first toolbar-level Windows text-to-speech pipeline for selected or staged text.

## User-visible behavior

- Clicking `TTS` reads text aloud.
- Clicking `TTS` again while playback is active stops the current playback.
- The `TTS` button shows active state while playback is running.
- Text source priority is:
  - internal editor selection,
  - live external UI Automation selection,
  - recent cached external UI Automation selection,
  - guarded clipboard fallback selection,
  - staged internal editor text.
- External selected text is mirrored into the internal editor before playback so the user has visible reading context.

## Implementation details

- Added `WindowsTextToSpeechService` as a toolbar-level Windows SAPI bridge.
- Wired the `TTS` toolbar action in `MainWindow`.
- Added view-model state for:
  - `IsTextToSpeechSpeaking`,
  - `TextToSpeechToolTip`,
  - `TextToSpeechButtonBackground`.
- Reused the existing `WindowsSelectionImportService` routes for external selection resolution instead of adding a second external-selection implementation.
- Preserved the existing overlay-row speaker implementation as a separate, row-scoped path.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsTextToSpeechService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/changes/2026-04-13_wsa_rt_013_windows_text_to_speech_selection_pipeline.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added. Toolbar TTS reuses existing local selection diagnostics when it has to resolve external selected text.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process was stopped after smoke)
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- Manual audio testing is still needed.
- The current baseline provides visible editor context by mirroring external text into the editor, but it does not yet synchronize word-by-word highlighting during SAPI playback.

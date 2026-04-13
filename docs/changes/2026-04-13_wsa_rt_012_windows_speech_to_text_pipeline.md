# WSA-RT-012_windows_speech_to_text_pipeline

## Summary

Added the first Windows speech-to-text pipeline behind the toolbar `MIC` action.

## User-visible behavior

- Clicking `MIC` starts a local Windows Speech Recognition dictation session.
- Clicking `MIC` again stops the session.
- The `MIC` button shows active state while listening.
- Final recognized phrases are inserted into the internal editor at the current caret position.
- Hypothesis/partial text is shown in the status line but is not inserted into the editor until Windows returns a final recognition result.

## Implementation details

- Added `WindowsSpeechToTextService`.
- Implemented speech recognition through a local PowerShell bridge around `System.Speech.Recognition.SpeechRecognitionEngine`.
- Kept the WPF app compile-time dependency-free from `System.Speech` because the assembly is available to Windows PowerShell on this machine but not directly resolvable by the `net9.0-windows` project.
- Added `SpeechToTextTranscript` as the transcript event contract.
- Wired `MainWindow` to:
  - start/stop the bridge from the `MIC` toolbar action,
  - update status for hypothesis text and bridge state,
  - insert final transcripts into the editor,
  - keep the editor focused after insertion.
- Added view-model state for:
  - `IsSpeechToTextListening`,
  - `SpeechToTextToolTip`,
  - `SpeechToTextButtonBackground`,
  - `InsertDictatedText()`.
- Added best-effort recognizer selection by active app language:
  - exact culture match,
  - same two-letter language match,
  - first installed recognizer fallback.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/SpeechToTextTranscript.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsSpeechToTextService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/changes/2026-04-13_wsa_rt_012_windows_speech_to_text_pipeline.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added. Speech hypotheses/final transcripts stay in the local app process and are inserted only when final recognition results are received.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process was stopped after smoke)
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)
- Installed recognizer check -> `en-GB Microsoft Speech Recognizer 8.0 for Windows (English - UK)`

## Known limitations / follow-up

- Manual microphone testing is still needed.
- The current machine only exposes `Microsoft Speech Recognizer 8.0 for Windows (English - UK)` / `en-GB` through the installed recognizer list. Danish dictation requires installing a Danish Windows Desktop Speech Recognition recognizer.
- Direct PowerShell probing showed that grammar loading can report `E_ACCESSDENIED` when Windows speech/audio permissions are not available to the desktop session. The app surfaces bridge stderr in the status line so this is visible during manual smoke.
- The current baseline inserts final recognized phrases at the caret. Fuller macOS-style active-range partial replacement can be refined after the basic microphone path is confirmed.

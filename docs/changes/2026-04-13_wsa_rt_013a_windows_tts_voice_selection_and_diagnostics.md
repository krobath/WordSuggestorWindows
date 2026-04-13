# WSA-RT-013A_windows_tts_voice_selection_and_diagnostics

## Summary

Added language-aware Windows TTS voice selection, persisted speech settings, and local TTS diagnostics.

## User-visible behavior

- Settings > `Generelt` > `Oplæsning` now exposes:
  - speech language mode,
  - system voice for the active WordSuggestor language,
  - reading speed,
  - reading strategy,
  - reading highlight mode,
  - shortcuts to Windows speech and language settings.
- The system voice picker is filtered by the active WordSuggestor language.
- If no installed SAPI Desktop voice matches the active language, Settings reports that clearly and the TTS toolbar action uses a visible fallback voice.
- Toolbar TTS status now names the selected/fallback voice path instead of failing silently.

## Implementation details

- Extended `AppSettingsSnapshot` with macOS-aligned speech fields:
  - `SpeechLanguageMode`,
  - `ReadingSpeedDelta`,
  - `ReadingStrategy`,
  - `ReadingHighlightMode`,
  - `SystemVoiceIdOverrideByLanguage`.
- Added `TtsVoiceOption` and `TtsSpeechOptions` models.
- Added `WindowsVoiceCatalogService` to enumerate SAPI Desktop voices and map their LCID language metadata to BCP-47 language codes.
- Updated `WindowsTextToSpeechService` so the PowerShell/SAPI bridge:
  - receives an explicit voice id when available,
  - applies app reading speed when not using system speech settings,
  - captures stderr/stdout,
  - writes token-safe diagnostics to `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log`.
- Updated toolbar TTS to resolve the speech voice through `MainWindowViewModel.CreateTextToSpeechOptions`.
- Kept the current TTS bridge on SAPI Desktop voices; OneCore/neural voice pack support remains follow-up work.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/TtsSpeechOptions.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/TtsVoiceOption.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsTextToSpeechService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsVoiceCatalogService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/changes/2026-04-13_wsa_rt_013a_windows_tts_voice_selection_and_diagnostics.md`

## Feature flags / settings impact

Adds new local Windows settings fields to the existing `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json` profile. Existing settings files remain readable because the new fields have defaults.

## Logging / telemetry impact

Adds local-only TTS diagnostics in `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log`. The log must not contain the spoken text; it stores only text length, selected language, voice metadata, fallback reason, exit code, and stderr summary.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Direct SAPI token smoke with `Microsoft Hazel Desktop - English (Great Britain)` and explicit rate -> `PASS` (`ExitCode=0`)
- Installed SAPI Desktop voice check -> `en-GB` Hazel and `en-US` Zira only; no `da-DK` voice installed on this machine
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process was stopped after smoke)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- This machine currently reports only English SAPI Desktop voices, so Danish TTS uses fallback until a Danish Windows Desktop voice is installed.
- OneCore voices are not used by the current SAPI bridge even if Windows exposes them elsewhere.
- `SpeechLanguageMode=autoDetect` is persisted but remains conservative in Windows: it uses the selected WordSuggestor language until a dedicated language detector is wired into the app layer.
- Full synchronized word/sentence highlight during playback remains follow-up work.

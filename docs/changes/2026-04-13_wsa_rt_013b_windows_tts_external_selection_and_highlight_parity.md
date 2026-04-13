# WSA-RT-013B_windows_tts_external_selection_and_highlight_parity

Date: `2026-04-13`

## Summary

Improved toolbar text-to-speech parity for external selections and editor reading feedback.

## User-Facing Changes

- `TTS` now writes UI-level diagnostics to `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log` as soon as the TTS action starts, even if selected text cannot be resolved.
- `Ctrl+Alt+T` is registered while WordSuggestor is running so users can invoke read-aloud from the currently focused external application without first clicking the WordSuggestor toolbar.
- Toolbar TTS now tries the last known external target window via Windows UI Automation before falling back to cached selection and clipboard copy.
- The clipboard fallback now verifies foreground activation, retries activation/copy once, and logs `GetLastWin32Error` when `SendInput` reports zero keyboard input events.
- During TTS playback, the internal editor applies a light-blue background to the active token and clears it when playback stops.
- Internal editor selections are collapsed while speaking so the highlight remains visible, then restored after playback stops.

## Files Changed

- `src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `docs/Plan.md`
- `docs/UiParityPlan.md`
- `docs/ParityMatrix.md`
- `docs/ManualSmoke.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (GUI launch smoke; `tts-flow.log` recorded `Global TTS hotkey registered: Ctrl+Alt+T.`)

## Voice Backend Note

After the user installed a Danish Windows voice, local registry inspection found `Microsoft Helle - Danish (Denmark)` under `HKLM\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens`. The current WordSuggestor Windows TTS bridge still uses SAPI Desktop voices, where this machine currently exposes only:

- `Microsoft Hazel Desktop - English (Great Britain)`
- `Microsoft Zira Desktop - English (United States)`

That means this sprint improves selection capture, diagnostics, and reading highlight parity, but Danish OneCore voice playback still needs a follow-up speech-backend sprint.

## Known Limitations

- The current reading highlight uses estimated timing around the SAPI process bridge. It provides visible macOS-like reading context, but it is not yet driven by exact speech word-boundary callbacks.
- If `Ctrl+Alt+T` conflicts with another app-level global hotkey, registration failure is logged to `tts-flow.log`.

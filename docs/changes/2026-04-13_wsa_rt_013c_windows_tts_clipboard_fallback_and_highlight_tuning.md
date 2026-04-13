# WSA-RT-013C_windows_tts_clipboard_fallback_and_highlight_tuning

Date: `2026-04-13`

## Summary

Fixed the external-app clipboard fallback path observed in VS Code and tuned the interim editor reading highlight.

## User-Facing Changes

- Selected text in apps that require clipboard fallback should no longer fail because of a malformed `SendInput` call.
- The internal editor reading highlight now uses a stronger light-blue background and stays active longer for short text, making it easier to see during read-aloud.

## Files Changed

- `src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `docs/Plan.md`
- `docs/UiParityPlan.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- SAPI voice probe confirmed the installed Danish `Microsoft Helle - Danish (Denmark)` OneCore voice is not returned by `SAPI.SpVoice.GetVoices()`.

## Known Limitations

- The current bridge still uses SAPI Desktop voices. Danish OneCore voice playback needs a separate OneCore/WinRT or replacement speech-backend sprint.
- External-app selected text capture still needs manual smoke in VS Code/Edge because Windows foreground and clipboard behavior depends on app focus state.

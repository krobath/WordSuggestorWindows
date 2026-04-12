# WSA-RT-011B_windows_ocr_snipping_tool_protocol_callback

## Summary

Replaced the fragile OCR hotkey-injection approach with Microsofts Snipping Tool protocol callback flow.

## User-visible behavior

- Clicking `OCR` now launches the Snipping Tool capture protocol instead of sending `Win+Shift+S`.
- WordSuggestor registers a per-user `wordsuggestor-ocr:` callback protocol before launching the capture.
- After the user selects a region, Snipping Tool should redirect back to WordSuggestor with a shared-storage token.
- WordSuggestor redeems the token, runs OCR on the returned image, copies recognized text to the clipboard, and imports it into the internal editor.

## Implementation details

- Added `OcrScreenClipCallback` for parsed callback state.
- Added `WindowsOcrCallbackBridge` for callback startup handling:
  - detects `wordsuggestor-ocr:` startup arguments,
  - defensively reconstructs callback URI arguments that may be split around `&`,
  - parses `code`, `reason`, `token`, and correlation id,
  - persists callback URI data to `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks`,
  - writes non-token diagnostics to `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log`.
- Moved callback handling before normal WPF startup in `App.OnStartup`, so callback launches can exit without opening the main window.
- Updated `WindowsOcrService` to:
  - register the `wordsuggestor-ocr:` protocol handler under `HKCU\Software\Classes`,
  - launch `ms-screenclip://capture/image?...` with rectangle mode, enabled modes, `auto-save=false`, a correlation id, and redirect URI,
  - wait for the matching callback file,
  - redeem the returned shared-storage token through a local PowerShell/WinRT bridge,
  - OCR the redeemed image with the existing Windows OCR bridge,
  - copy recognized text to the clipboard and import it into the editor.
- Removed the `SendInput` / `Win+Shift+S` path from `WindowsOcrService`.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/App.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/OcrScreenClipCallback.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrCallbackBridge.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_011a_windows_ocr_screen_snip_invocation_fix.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_011b_windows_ocr_snipping_tool_protocol_callback.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added. OCR callback diagnostics are local only and intentionally avoid logging callback tokens.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch -> no recent `WordSuggestorWindows.App` crash event found
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- Manual confirmation is still needed because the Snipping Tool screen selection and redirect callback are interactive.
- The protocol registration is per-user under HKCU and does not require admin rights.
- The launch-smoke process was stopped after validation so the debug executable is not left locked.

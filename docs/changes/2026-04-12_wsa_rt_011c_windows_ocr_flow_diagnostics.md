# WSA-RT-011C_windows_ocr_flow_diagnostics

## Summary

Added token-safe local diagnostics for the Windows OCR flow so failures after the Snipping Tool callback can be isolated.

## User-visible behavior

- The OCR button still starts the same Snipping Tool region selection flow.
- If OCR fails after region selection, the app now writes a local flow log that can identify the failing stage.
- No recognized OCR text and no shared-storage callback token are written to the diagnostic logs.

## Implementation details

- Added `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log`.
- Logged UI-level OCR stages:
  - OCR toolbar action started,
  - WordSuggestor window hidden/restored for capture,
  - OCR result missing,
  - recognized text imported into the internal editor.
- Logged service-level OCR stages:
  - callback protocol registration,
  - Snipping Tool protocol launch,
  - callback wait/result,
  - shared-storage token bridge start/exit,
  - OCR bridge start/exit,
  - normalization character count,
  - clipboard copy failure,
  - temp-file and callback-file cleanup.
- Redacted the shared-storage token from bridge stderr before logging.
- Kept OCR text out of diagnostics; only character counts, line counts, paths, exit codes, and sanitized stderr summaries are logged.
- Changed OCR recognition to use the actual redeemed image path returned by the token bridge.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_011c_windows_ocr_flow_diagnostics.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

Local diagnostics were added in `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log`. This is not telemetry and is not sent anywhere.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)
- Application event log check did not show a useful recent OCR failure entry, so the new flow log is the intended diagnostic path for the next manual OCR test.

## Known limitations / follow-up

- Interactive Snipping Tool selection still needs manual verification.
- If OCR still returns to WordSuggestor without importing text, inspect `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` and `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log` together.

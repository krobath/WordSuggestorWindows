# WSA-RT-011D_windows_ocr_file_access_token_callback

## Summary

Updated the Snipping Tool callback parser to treat the observed `file-access-token` query parameter as the redeemable shared-storage token.

## User-visible behavior

- OCR still opens the Snipping Tool region selector.
- After selecting a region, WordSuggestor now redeems the `file-access-token` returned by Snipping Tool and runs OCR on that returned image.
- Clipboard-image and saved-screenshot fallbacks are not part of the primary OCR flow.

## Implementation details

- Changed `OcrScreenClipCallback.IsSuccess` so `code=200` is treated as a successful callback independent of whether token parsing has already succeeded.
- Added `file-access-token` as a recognized token query parameter in `WindowsOcrCallbackBridge`.
- Preserved `token` as the documented primary parameter and kept additional shared-storage aliases for compatibility.
- Kept token-safe callback diagnostics that log only query parameter names, token presence, and token length.
- Removed the clipboard-image fallback path from `WindowsOcrService`; when no redeemable token is present, the app now records a contract failure in `ocr-flow.log`.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/OcrScreenClipCallback.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrCallbackBridge.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/changes/2026-04-13_wsa_rt_011d_windows_ocr_file_access_token_callback.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

Local diagnostics remain in `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` and `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log`. They do not log OCR text or callback token values.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- Interactive Snipping Tool selection still needs manual confirmation after this token-key fix.
- If Snipping Tool changes the callback token parameter again, `ocr-callback.log` should show the non-sensitive query keys needed to update the parser.

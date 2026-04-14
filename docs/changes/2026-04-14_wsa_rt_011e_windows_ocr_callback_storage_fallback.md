# WSA-RT-011E_windows_ocr_callback_storage_fallback

## Summary

Fixed the OCR callback startup crash by making callback-file persistence fall back to a writable temp directory when `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks` cannot be written.

## User-visible behavior

- Clicking `OCR` should no longer leave WordSuggestor hidden for 90 seconds because the callback helper process crashed before persisting the callback.
- The callback helper can now persist the `wordsuggestor-ocr:` callback into a fallback temp directory and let the waiting app instance resume OCR processing.
- The user-facing OCR flow remains the same:
  - hide WordSuggestor,
  - start Snipping Tool,
  - receive callback,
  - OCR the returned image,
  - import text into the internal editor.

## Root-cause evidence

- OCR flow logs on `2026-04-14` showed `Capture stopped: no callback before timeout.` after the app hid itself.
- Starting `WordSuggestorWindows.App.exe` directly with a callback URI argument exited with `0xE0434352`.
- Running the managed app directly exposed the real exception:
  - `System.UnauthorizedAccessException`
  - failing path: `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks\<correlation>.uri`
  - throw site: `WindowsOcrCallbackBridge.TryPersistStartupCallback(...)`
- Local shell tests showed that `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks` was not writable in practice on this machine, even though ACLs looked normal.

## Implementation details

- Reworked `WindowsOcrCallbackBridge` to use multiple candidate callback directories:
  - `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks`
  - `%TEMP%\WordSuggestor\ocr-callbacks`
- Added writable-directory probing so OCR chooses a callback location it can actually write to.
- Changed callback files from `.uri` to `.callback`.
- Updated callback persistence to try each candidate directory in order and continue safely if one location throws `UnauthorizedAccessException` or `IOException`.
- Updated callback read/delete logic to check all candidate callback-file locations.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrCallbackBridge.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-14_wsa_rt_011e_windows_ocr_callback_storage_fallback.md`
- `WordSuggestorDocs/90_agent_ops/handoffs/windows_track.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

- Existing OCR diagnostics remain local-only in:
  - `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log`
  - `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log`
- Callback-file storage may now occur under `%TEMP%\WordSuggestor\ocr-callbacks`.
- No OCR text or callback token values are logged.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Direct callback startup smoke:
  - `WordSuggestorWindows.App.exe "wordsuggestor-ocr://callback/..."`
  - result: `PASS` (`exit code 0`)
  - callback file persisted under `%TEMP%\WordSuggestor\ocr-callbacks`
- Temp callback-directory write smoke -> `PASS`

## Known limitations / follow-up

- The full interactive Snipping Tool OCR flow still needs to be manually re-tested after this fix.
- The callback diagnostics log still deserves a follow-up cleanup pass so it records the newly used fallback path more explicitly during callback persistence.

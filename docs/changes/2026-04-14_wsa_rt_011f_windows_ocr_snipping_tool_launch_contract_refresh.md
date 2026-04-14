# WSA-RT-011F_windows_ocr_snipping_tool_launch_contract_refresh

## Summary

Refreshed the OCR Snipping Tool launch request so it follows the current `ms-screenclip` documentation more conservatively and logs the exact launch contract used during OCR capture.

## User-visible behavior

- OCR still starts by hiding WordSuggestor and launching Snipping Tool.
- The launch request is now intentionally minimal so Snipping Tool is more likely to enter active rectangle snip mode instead of merely opening its window.
- OCR diagnostics now make it clearer which launch URI was used and which callback-file locations the app is monitoring.

## Root-cause evidence

- Local OCR tests on `2026-04-14` showed `processStarted=True` but no callback, and the user reported that Snipping Tool was not actually entering a usable snip flow.
- Microsoft Learn's current `Launch Snipping Tool` documentation dated `2025-02-25` emphasizes a required mode plus the core request parameters and warns that invalid requests may be ignored.
- The previous OCR URI included optional parameters (`api-version`, `enabledModes`, `auto-save`) that were not necessary for basic rectangle snipping.

## Implementation details

- Simplified the OCR capture URI to:
  - `rectangle`
  - `user-agent`
  - `x-request-correlation-id`
  - `redirect-uri`
- Removed these optional parameters from the OCR launch path:
  - `api-version`
  - `enabledModes`
  - `auto-save`
- Added a diagnostic line that logs the exact Snipping Tool URI used for the OCR launch.
- Updated callback-wait diagnostics so OCR logs all candidate callback-file paths instead of only the preferred one.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrCallbackBridge.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-14_wsa_rt_011f_windows_ocr_snipping_tool_launch_contract_refresh.md`
- `WordSuggestorDocs/90_agent_ops/handoffs/windows_track.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

- `ocr-flow.log` now logs:
  - `Launching Snipping Tool URI: ...`
  - all callback-file paths being monitored during callback wait
- No OCR text or callback token values are logged.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Code path review against Microsoft Learn `Launch Snipping Tool` (`2025-02-25`) -> `PASS`

## Known limitations / follow-up

- Full interactive Snipping Tool confirmation is still required on this machine.
- If Snipping Tool still opens without entering active snip mode, the next step should be a capability/discover probe or a compatibility fallback path, not immediately reintroducing extra optional parameters.

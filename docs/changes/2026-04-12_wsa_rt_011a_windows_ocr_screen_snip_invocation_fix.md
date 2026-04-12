# WSA-RT-011A_windows_ocr_screen_snip_invocation_fix

## Summary

Fixed the OCR screen snip invocation so the toolbar action starts the direct Windows selection overlay instead of opening the Snipping Tool window.

## User-visible behavior

- Clicking `OCR` now sends the Windows `Win+Shift+S` screen snip shortcut.
- WordSuggestor still hides before the capture and returns after the OCR flow.
- Recognized text is still copied to the clipboard and imported into the internal editor.

## Implementation details

- Replaced the previous `ms-screenclip:` URI launch in `WindowsOcrService` with a `SendInput` keyboard sequence:
  - Windows key down
  - Shift down
  - `S` down/up
  - Shift up
  - Windows key up
- Preserved the existing clipboard sentinel, clipboard image polling, OCR bridge, normalization, and editor ingest behavior.
- Updated the OCR docs to describe `Win+Shift+S` as the invocation path.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_011_windows_ocr_snip_pipeline.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_011a_windows_ocr_screen_snip_invocation_fix.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch -> no recent `WordSuggestorWindows.App` crash event found
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- Manual confirmation is still needed because the screen snip overlay itself is interactive.
- The launch-smoke process was stopped after validation so the debug executable is not left locked.

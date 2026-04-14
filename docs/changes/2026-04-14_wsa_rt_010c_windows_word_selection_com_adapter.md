# WSA-RT-010C_windows_word_selection_com_adapter

## Summary

Added a Microsoft Word-specific external-selection adapter so `TXT` and `TTS` can resolve the current Word selection without relying on the crash-prone clipboard fallback path.

## User-visible behavior

- Import from Microsoft Word should now use the text the user actually selected in Word, without depending on synthetic `Ctrl+C`.
- `TTS` should mirror the selected Word text into the internal editor and start playback through the same Word-specific selection route.
- Non-Word apps still use the existing Windows selection order:
  - live UI Automation selection,
  - cached same-window selection,
  - guarded clipboard fallback where needed.

## Root-cause evidence

- Local diagnostics showed `ClipboardFallback Success` against `WINWORD.EXE` immediately before Word instability.
- Windows Application events on `2026-04-14` recorded repeated `WINWORD.EXE` crashes in `combase.dll` with exception `0xc0000005`.
- The old path also showed long clipboard-restore stalls after Word selection import, which matches the instability window seen in manual smoke.

## Implementation details

- Added a Word-specific COM selection adapter in `WindowsSelectionImportService`.
- The service now detects `WINWORD.EXE` and attempts to read `Word.Application.Selection.Text` through a small PowerShell COM bridge.
- The COM bridge also returns the active Word window handle so the imported selection remains tied to the correct source window.
- Clipboard fallback now skips Microsoft Word explicitly and logs `SkippedForWord` rather than injecting `Ctrl+C` into Word.
- Existing generic selection logic for non-Word apps was preserved.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/SelectionImportCompatibilityMatrix.md`
- `WordSuggestorWindows/docs/changes/2026-04-14_wsa_rt_010c_windows_word_selection_com_adapter.md`
- `WordSuggestorDocs/90_agent_ops/handoffs/windows_track.md`
- `WordSuggestorDocs/90_agent_ops/AGENT_BOARD.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

- `selection-import.log` can now emit `OfficeSelection` diagnostics in addition to the existing `UIA` and `ClipboardFallback` stages.
- Clipboard fallback against Word should now log `SkippedForWord` instead of `Success`.
- No selected-text content is written to logs.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Windows Application log review after the reproduced Word failure -> `PASS` (root cause evidence collected)

## Known limitations / follow-up

- Manual Word smoke still needs to confirm end-to-end stability after the adapter change.
- If Outlook or other Office hosts show the same instability pattern, they may need their own Office-specific adapter instead of generic clipboard fallback.

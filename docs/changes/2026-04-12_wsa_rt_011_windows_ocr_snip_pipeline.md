# WSA-RT-011_windows_ocr_snip_pipeline

## Summary

Implemented the first Windows OCR toolbar pipeline using Windows screen snip, Windows OCR, clipboard text copy, and internal-editor ingest.

## User-visible behavior

- Clicking `OCR` now starts the Windows screen snip flow.
- WordSuggestor hides while the user selects a screen region, so the toolbar is less likely to be captured accidentally.
- When Windows places the captured image on the clipboard, WordSuggestor runs OCR on that image.
- Recognized text is copied back to the clipboard and imported into the internal editor.
- If the snip is cancelled or OCR finds no text, WordSuggestor returns to the toolbar and shows a status message.

## Implementation details

- Added `OcrImportResult` as the OCR import contract.
- Added `WindowsOcrService` for:
  - launching the Windows screen snip overlay through `Win+Shift+S` after the `WSA-RT-011A` follow-up fix,
  - placing a temporary clipboard sentinel before capture,
  - waiting for the clipboard image produced by Windows screen snip,
  - saving the clipboard image as a temporary PNG,
  - running Windows OCR through a local PowerShell/WinRT bridge,
  - copying recognized text back to the clipboard,
  - restoring the previous clipboard if the capture/OCR path does not produce text,
  - deleting temporary files.
- Updated `MainWindow` so the `OCR` toolbar action runs the OCR flow and imports text through the same editor path used by selected-text import.
- Removed the obsolete OCR placeholder status branch from `MainWindowViewModel`.
- Kept the WPF project on `net9.0-windows`; the implementation avoids a new offline-unavailable `Microsoft.Windows.SDK.NET.Ref` restore dependency by using a runtime OCR bridge.
- Added OCR text normalization for line endings, soft wraps, simple list breaks, and OCR hyphenation markers.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/OcrImportResult.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_011_windows_ocr_snip_pipeline.md`

## Feature flags / settings impact

No feature flags or persisted settings were added.

## Logging / telemetry impact

No telemetry was added. OCR text is copied to the local clipboard and staged in the internal editor, matching the macOS product flow.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Direct Windows OCR API smoke against a generated PNG containing `Hej OCR test` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch -> no recent `WordSuggestorWindows.App` crash event found
- `git -C .\WordSuggestorWindows diff --check` -> `PASS` (only Git CRLF/LF normalization warnings on existing Windows files)
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- Direct PDF-file OCR import is not implemented in this sprint. Visible PDF content can be captured through the screen snip path.
- The implementation depends on Windows screen snip placing a bitmap on the clipboard and Windows OCR being available for the current user profile.
- The first direct compile-time WinRT approach required `Microsoft.Windows.SDK.NET.Ref`, which was not available offline in this workspace; the final implementation uses a local PowerShell/WinRT bridge instead.
- `WSA-RT-011A` replaced the original `ms-screenclip:` URI invocation with synthetic `Win+Shift+S`, because the URI opened the Snipping Tool window on this Windows installation.
- The launch-smoke process was stopped after validation so the debug executable is not left locked.

# WSA-RT-013E_windows_tts_reading_highlight_visibility_and_restore

Date: `2026-04-14`

## Summary

Improved the Windows TTS reading highlight so the spoken range becomes visibly marked inside the internal editor and the previous caret/selection is restored after playback.

## What changed

- `MainWindow` now stores the editor's previous caret/selection before speech highlight starts.
- The active speech range is now rendered through both:
  - background tint on the document range
  - the `RichTextBox` selection renderer with a light-blue selection brush
- Speech highlight movement no longer feeds back into normal selection-change handling while playback is active.
- The Windows highlight scheduler now respects `ReadingHighlightMode` for:
  - `none`
  - `word`
  - `sentence`

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`

## Known note

- Highlight timing is still estimated from text length and speed settings because the current Windows bridge does not yet expose native word-boundary callbacks comparable to macOS `AVSpeechSynthesizerDelegate`.

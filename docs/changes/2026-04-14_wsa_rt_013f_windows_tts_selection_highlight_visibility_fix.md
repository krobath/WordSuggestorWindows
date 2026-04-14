# WSA-RT-013F_windows_tts_selection_highlight_visibility_fix

Date: `2026-04-14`

## Summary

Fixed a Windows TTS highlight bug where the editor visibly moved the caret during playback but failed to keep the active spoken range selected long enough for the user to see the light-blue highlight.

## What changed

- Removed the extra caret reset that collapsed the active speech selection immediately after `Selection.Select(...)`.
- Kept focus on the editor before applying the active speech selection so the built-in selection renderer can stay visible.
- Left the existing document background tint in place as a second highlight layer behind the selection renderer.

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`

## Known note

- Exact speech-follow timing is still estimated in the current Windows implementation. The selection/highlight visibility bug is separate from the remaining timing-precision limitation.

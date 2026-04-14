# WSA-RT-013G_windows_tts_precise_onecore_boundary_highlighting

Date: `2026-04-14`

## Summary

Moved Windows OneCore TTS highlighting from an estimated total-duration timer to speech-boundary metadata emitted by the synthesized speech stream.

## What changed

- `TtsSpeechOptions` now carries the active `ReadingHighlightMode` into the Windows TTS bridge.
- The OneCore speech bridge now requests word or sentence boundary metadata from `SpeechSynthesizer.Options`.
- The bridge emits boundary cues back to the app, and the Windows editor highlight scheduler now follows those exact OneCore cue offsets and start times.
- The older estimated timing scheduler remains as fallback only for non-OneCore playback paths.

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`

## Known note

- This improves precision on the OneCore path. If playback drops to SAPI/Desktop fallback, Windows still uses the older estimated highlight scheduler.

# WSA-RT-013I Windows TTS precise boundary offset alignment

Date: `2026-04-14`
Owner: `Windows track`

## Why

Manual smoke showed that the visible reading highlight could be shifted far ahead of the word actually being spoken.

The likely cause was that the precise OneCore path synthesized SSML-wrapped text while the Windows editor highlight expected boundary offsets for the raw editor text.

## What changed

- changed the precise OneCore path from `SynthesizeSsmlToStreamAsync(...)` to `SynthesizeTextToStreamAsync(...)`
- moved reading-speed control for the precise OneCore path to `SpeechSynthesizer.Options.SpeakingRate`
- preserved metadata-driven cue emission and the existing WPF highlight scheduler
- removed the SSML-wrapper dependency from cue-position alignment

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1`

## Notes

- This sprint keeps precise boundary metadata; it does not revert to the old duration-estimate-only highlight model.
- Final verification is manual because the bug appears during live playback alignment, not in compile-time tests.

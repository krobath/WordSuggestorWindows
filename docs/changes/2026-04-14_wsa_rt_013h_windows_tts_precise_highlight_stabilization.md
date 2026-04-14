# WSA-RT-013H Windows TTS precise highlight stabilization

Date: `2026-04-14`
Owner: `Windows track`

## Why

Manual smoke after `WSA-RT-013G` exposed two real issues in the new precise OneCore TTS path:

- some texts caused the inline PowerShell speech bridge to fail before playback started
- repeated highlight mutations in the WPF editor increased pressure in `PresentationCore` and aligned with a freeze/crash report

## What changed

- replaced the inline OneCore PowerShell command with a temp `.ps1` bridge plus temp JSON payload
- kept precise OneCore boundary cue emission intact while making arbitrary text safer to pass into the bridge
- suppressed PowerShell progress output in the OneCore bridge
- cleaned up generated temp artifacts after playback
- removed per-cue document background mutations from the internal editor highlight path
- retained the visible `RichTextBox` selection-based light-blue reading highlight

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1`

## Notes

- This sprint stabilizes the precise highlight path without changing `WordSuggestorCore`.
- Final confirmation remains manual because the original failure required real GUI playback to reproduce.

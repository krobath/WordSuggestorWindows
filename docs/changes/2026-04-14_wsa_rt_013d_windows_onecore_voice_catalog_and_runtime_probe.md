# WSA-RT-013D_windows_onecore_tts_voice_catalog_and_playback

Date: `2026-04-14`

## Summary

Implemented the first delivery slice of `WSA-RT-013D`: OneCore voice discovery, source-aware voice selection, and OneCore-first runtime dispatch with an explicit SAPI Desktop fallback.

## What changed

- `WindowsVoiceCatalogService` now reads both:
  - `HKLM\SOFTWARE\Microsoft\Speech\Voices\Tokens`
  - `HKLM\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens`
- Voice options now carry `Source`, so WordSuggestor can distinguish `OneCore` from `SAPI Desktop`.
- The settings voice picker now shows source-aware labels and can surface Danish OneCore voices such as `Microsoft Helle - Danish (Denmark)`.
- `TtsSpeechOptions` and the toolbar TTS path now pass the selected voice source through to runtime dispatch.
- `WindowsTextToSpeechService` now:
  - probes whether OneCore playback is usable on the current host
  - attempts OneCore playback when the selected voice comes from `Speech_OneCore`
  - falls back to SAPI Desktop with a clear fallback reason when OneCore is unavailable
- The settings window now warns when a language-matching OneCore voice exists but playback is not currently usable on the host.
- Toolbar status text now reports the chosen playback voice and backend source.

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Registry validation on this machine confirms `Microsoft Helle - Danish (Denmark)` exists under `HKLM\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens`
- Direct WinRT speech probe on this machine still fails with `Internal Speech Error`

## Current blocker

- The OneCore catalog/runtime plumbing is now in place, but actual OneCore playback is still blocked on this host by WinRT speech initialization failure.
- Until that host issue is resolved, toolbar `TTS` continues to fall back to SAPI Desktop playback.

## Docs updated

- `docs/Plan.md`
- `docs/UiParityPlan.md`
- `docs/ParityMatrix.md`
- `docs/ManualSmoke.md`
- `WordSuggestorDocs/90_agent_ops/handoffs/windows_track.md`
- `WordSuggestorDocs/90_agent_ops/AGENT_BOARD.md`

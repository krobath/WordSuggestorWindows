# WSA-RT-013D_windows_onecore_tts_voice_catalog_and_playback

Date: `2026-04-14`

## Summary

Planned the next Windows TTS sprint needed to make installed Danish Windows voices visible and usable in WordSuggestorWindows.

## Problem Statement

- The current Windows voice catalog only reads SAPI Desktop voices.
- The current Windows playback bridge also uses only `SAPI.SpVoice`.
- The installed Danish voice `Microsoft Helle - Danish (Denmark)` is present under `Speech_OneCore`, not under the SAPI Desktop voice root.
- As a result, the voice is neither visible in settings nor selectable for playback.

## Planned Sprint Outcome

- Settings should discover both `SAPI Desktop` and `OneCore` voices.
- `DA` should expose `Microsoft Helle - Danish (Denmark)` in `Generelt > Oplæsning`.
- Toolbar `TTS` should be able to read Danish text with the selected OneCore voice.
- Diagnostics should record which backend/source was used.

## Docs Updated

- `docs/Plan.md`
- `docs/UiParityPlan.md`
- `docs/ParityMatrix.md`
- `docs/ManualSmoke.md`
- `WordSuggestorDocs/90_agent_ops/handoffs/windows_track.md`
- `WordSuggestorDocs/90_agent_ops/AGENT_BOARD.md`

## Risk Note

Earlier shell-level WinRT speech probes returned `Internal Speech Error`, so the implementation sprint should begin with an app-hosted OneCore playback spike before the full runtime/settings migration is committed.

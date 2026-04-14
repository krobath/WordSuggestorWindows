# WSA-TS-003 Windows selection import Word crash instrumentation

Date: `2026-04-14`
Owner: `Windows track`

## Why

Manual testing against Microsoft Word showed that clicking `TTS` after selecting text in Word could destabilize the interaction:

- Word could crash
- WordSuggestor could appear hung briefly
- the import/playback path still sometimes completed afterwards

Existing diagnostics were not specific enough to distinguish route choice, target-app identity, foreground behavior, and clipboard timing.

## What changed

- enriched `selection-import.log` with target window metadata:
  - hwnd
  - pid
  - process name
  - responding state
  - window title
  - window class
- added clipboard fallback timing diagnostics for:
  - copy success latency
  - clipboard restore latency
  - return-to-WordSuggestor foreground request
- added route-level diagnostics in `MainWindow` so `TXT`/`TTS` log which external selection route was chosen

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1`

## Notes

- Current logs indicate the target app crash occurs in `WINWORD.EXE` / `combase.dll`, but the new instrumentation is intended to show exactly what WordSuggestor was doing immediately before that crash.

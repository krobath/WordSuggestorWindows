# WSA-RT-010B Windows selection snapshot stability and UIA guardrails

Date: `2026-04-14`
Owner: `Windows track`

## Why

Manual testing showed two related Windows problems:

- toolbar actions could import the wrong external text after focus moved from the source app into WordSuggestor
- UI Automation polling could crash the app through an unhandled `System.ArgumentException` from `UIAutomationClientSideProviders`

## What changed

- added source window handles to `SelectionImportResult`
- tied cached external selections to the external app window they came from
- changed `TXT` and `TTS` external selection resolution to prefer:
  - live external selection when another app is still foreground
  - recent cached external selection from the same window
  - last external target window selection
  - guarded clipboard fallback
- removed the global focused-element dependency from the foreground selection path
- added defensive exception handling around UI Automation selection reads
- wrapped background external selection polling so provider failures are logged instead of crashing the app

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1`

## Notes

- This sprint does not remove the app-specific compatibility differences in Windows UI Automation, but it should make WordSuggestor's own behavior much more deterministic and crash-resistant.

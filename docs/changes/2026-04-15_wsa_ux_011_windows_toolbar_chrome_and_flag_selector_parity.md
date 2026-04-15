# WSA-UX-011_windows_toolbar_chrome_and_flag_selector_parity

Date: `2026-04-15`
Status: `Done`

## Why

The Windows app already matched the macOS product structurally, but the toolbar and expanded command row still looked too much like a functional placeholder. The most visible gaps were:

- text-first toolbar action buttons instead of icon-first controls
- a text-only language selector instead of the flag-based selector seen in macOS
- expanded command toggles that did not yet resemble the rounded command pills from macOS closely enough

## Implemented

- Reworked the Windows toolbar button chrome so the top-row actions now render as compact icon-first controls.
- Updated the top toolbar actions `WL`, `TXT`, `OCR`, `MIC`, `TTS`, `INS`, and settings to use icon glyphs while preserving their existing click handlers, tooltips, and state bindings.
- Reworked the expanded editor command row so `Farver`, `Semantik`, and `Tegnsætning` read as rounded command pills closer to the macOS hierarchy.
- Extended `LanguageOption` with flag presentation metadata.
- Wired the supported Windows languages to specific flag-pattern definitions in the CLI suggestion provider.
- Re-templated the toolbar language `ComboBox` so both the selected state and drop-down items render flag visuals instead of text-first labels.

## Guardrails kept

- No `WordSuggestorCore` changes.
- No macOS code changes.
- No language-routing or command-behavior changes; this sprint only changes presentation/templates and visual metadata.

## Validation

- `dotnet build .\WordSuggestorWindows\src\WordSuggestorWindows.App\WordSuggestorWindows.App.csproj --no-restore /p:UseAppHost=false /p:OutDir=C:\Users\mswin01\Code\WordSuggestor\.build-validate\WordSuggestorWindows.App\`
- `git -C .\WordSuggestorWindows diff --check`
- `git -C .\WordSuggestorDocs diff --check`

## Notes

- This implementation uses Windows-native glyphs plus XAML-rendered flag patterns rather than imported raster flag assets.
- Final acceptance still depends on manual UI comparison against the macOS screenshots in a live Windows session.

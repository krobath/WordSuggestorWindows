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
- Updated the top toolbar actions `WL`, `TXT`, `OCR`, `MIC`, `TTS`, `INS`, and settings to use icon glyphs or compact custom icon visuals while preserving their existing click handlers, tooltips, and state bindings.
- Adjusted the `OCR` action to use a more scan/snipping-oriented custom icon instead of a folder-like glyph.
- Reworked the expanded editor command row so `Farver`, `Semantik`, and `Tegnsætning` read as rounded command pills closer to the macOS hierarchy.
- Added explicit inline icons to `Farver`, `Semantik`, and `Tegnsætning` so they no longer render as text-only pills.
- Replaced the approximated command-row icon drawings with cropped macOS-derived screenshot assets for `Farver`, `Semantik`, `Tegnsætning`, and the adjacent refresh action.
- Registered the new `Assets/MacOSCommandIcons/*.png` files as WPF `Resource` items so the icons resolve correctly at runtime when the app is launched with `dotnet run`.
- Switched the command-row image bindings to explicit `pack://application:,,,/Assets/...` URIs so the four icons resolve reliably in the running WPF application.
- Extended `LanguageOption` with flag presentation metadata.
- Wired the supported Windows languages to specific flag-pattern definitions in the CLI suggestion provider.
- Re-templated the toolbar language `ComboBox` so both the selected state and drop-down items render smaller flag visuals instead of text-first labels.

## Guardrails kept

- No `WordSuggestorCore` changes.
- No macOS code changes.
- No language-routing or command-behavior changes; this sprint only changes presentation, templates, and visual metadata.

## Validation

- `dotnet build .\WordSuggestorWindows\src\WordSuggestorWindows.App\WordSuggestorWindows.App.csproj --no-restore /p:UseAppHost=false /p:OutDir=C:\Users\mswin01\Code\WordSuggestor\.build-validate\WordSuggestorWindows.App\`
- `git -C .\WordSuggestorWindows diff --check`
- `git -C .\WordSuggestorDocs diff --check`

## Notes

- This implementation uses Windows-native glyphs plus XAML-rendered flag patterns for the language selector, while the command-row action icons now use cropped raster assets derived from the macOS screenshot reference.
- Final acceptance still depends on manual UI comparison against the macOS screenshots in a live Windows session.

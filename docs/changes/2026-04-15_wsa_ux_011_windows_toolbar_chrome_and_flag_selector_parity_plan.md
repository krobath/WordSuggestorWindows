# WSA-UX-011_windows_toolbar_chrome_and_flag_selector_parity

Date: `2026-04-15`
Status: `Planned`

## Why

The current Windows toolbar preserves the right feature ordering and runtime behavior, but it still reads as a functional Windows placeholder rather than the same polished product as the macOS build. The most visible gaps are:

- text-first toolbar action buttons instead of macOS-like icon-first affordances
- a text-only language selector instead of a flag-based selector
- expanded editor command toggles that do not yet resemble the macOS command-row pills closely enough

## Planned scope

- Restyle the top-toolbar action buttons `WL`, `TXT`, `OCR`, `MIC`, `TTS`, and `INS`.
- Restyle the expanded editor command toggles `Farver`, `Semantik`, and `Tegnsætning`.
- Introduce a flag-based toolbar language selector for supported languages.

## Planned implementation

- Add dedicated WPF styles/templates for toolbar action-button chrome.
- Add dedicated WPF styles/templates for expanded command-row toggle chrome.
- Extend the language-option presentation model with flag imagery metadata.
- Add flag assets and bind the toolbar selector so both the selected item and dropdown items show the correct flag.
- Preserve the existing command routing, tooltips, accessibility labels, and language-pack availability logic.

## Guardrails

- Windows must still feel native; this is a product-parity sprint, not an AppKit imitation sprint.
- Prefer Windows-native symbols when they are low-confusion and visually appropriate.
- Reuse SF Symbols or exported macOS assets only when Windows does not provide a strong equivalent.
- Do not change `WordSuggestorCore` or macOS code for this sprint.

## Validation target

- Manual UI comparison against the macOS toolbar/editor screenshots already captured in the Windows planning work.
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1`

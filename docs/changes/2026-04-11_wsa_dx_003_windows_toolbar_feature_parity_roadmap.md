# WSA-DX-003_windows_toolbar_feature_parity_roadmap

## Summary

Captured the toolbar feature parity analysis as an actionable Windows roadmap.

## User-visible behavior

No app behavior changed. This is a documentation-only sprint that defines how the existing toolbar placeholders should be implemented in later Windows sprints.

## Implementation details

- Added `WSA-DX-003_windows_toolbar_feature_parity_roadmap` to the active Windows plan.
- Added planned sprint entries for language selection, selected-text import, OCR, speech-to-text, text-to-speech, insights, and settings parity.
- Added a toolbar feature parity table to the UI parity plan.
- Updated the parity matrix with per-button status so the top toolbar can be tracked without rereading the macOS source each time.
- Normalized the Windows settings sprint to `WSA-UX-010_windows_settings_window_parity`, which is the next available Windows UX number after `WSA-UX-009`.

## Files changed

- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/changes/2026-04-11_wsa_dx_003_windows_toolbar_feature_parity_roadmap.md`

## Feature flags / settings impact

No feature flags or settings were added or changed.

## Logging / telemetry impact

No logging or telemetry behavior changed.

## Validation performed

- Documentation-only review; no build was run because the sprint only changes Markdown planning documents.

## Known limitations / follow-up

- The word-list manager remains placeholder parity because the macOS toolbar action is currently a TODO and the settings tab states that administration of domain lists will come later.
- Each implementation sprint must still add its own `docs/changes/` note and validation record when code is changed.

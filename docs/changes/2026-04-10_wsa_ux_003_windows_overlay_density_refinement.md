# Windows Overlay Density Refinement

- Date: `2026-04-10`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `done`
- Sprint ID: `WSA-UX-003_windows_overlay_density_refinement`

## Summary

Refined the floating suggestion overlay so the first page can present all 10 candidates without forcing an immediate vertical scroll, while keeping the existing paging, placement controls, and two-line candidate metadata layout.

## User-Visible Behavior

- The suggestion overlay is taller and gives more space to the list itself.
- Each candidate row is visually denser and closer to the proportions seen in the macOS screenshot.
- Header and footer chrome take less space, so the first page can fit the full 10-candidate set more comfortably.
- The overlay still preserves:
  - static and follow-caret placement controls
  - page controls
  - candidate shortcut affordances

## Implementation Details

- Increased the overlay window height from `438` to `520`.
- Reduced header padding and title/page-count typography.
- Reduced per-row margins, padding, and text sizes while preserving the two-line row structure.
- Added text trimming to candidate term and kind fields so long values do not force awkward vertical growth.
- Reduced footer button and instruction text sizes so more height is available to the list content.
- Fixed overlay helper copy to use proper Danish characters in the footer text.

## Files Changed

- `src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml` - compacted row layout, reduced chrome density, and expanded usable list area
- `docs/Plan.md` - recorded `WSA-UX-003` as a completed UI refinement sprint
- `docs/ManualSmoke.md` - added the 10-visible-candidates smoke expectation
- `docs/changes/2026-04-10_wsa_ux_003_windows_overlay_density_refinement.md` - this note

## Feature Flags / Settings

- No settings or feature flags added.

## Logging / Telemetry

- No telemetry added.

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild`
  - `Get-Process WordSuggestorWindows.App | Select-Object ProcessName, Id, Responding`
- Result:
  - WPF app build: `PASS`
  - Refined overlay launch: `PASS`
  - Process check after launch: `WordSuggestorWindows.App` remained responsive

## Known Limitations / Follow-up

- This sprint only adjusts the density and fit of the internal-editor overlay.
- External-app overlay anchoring is still future work in `WSA-RT-003`.
- Candidate rows still use the current Windows baseline metadata layout rather than the full final macOS-inspired styling model.

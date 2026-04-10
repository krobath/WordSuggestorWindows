# Windows Internal Editor Surface Parity

- Date: `2026-04-10`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `done`
- Sprint ID: `WSA-UX-002_windows_internal_editor_surface_parity`

## Summary

Reworked the expanded Windows editor surface so it now reflects the same core information architecture as the macOS editor: command row, contextual editor header, structured status metrics, and analyzer legend/panel framing, while keeping the floating suggestion overlay intact.

## User-Visible Behavior

- The expanded editor no longer feels like a generic textbox inside the toolbar shell.
- The editor now exposes a clearer Windows-native structure with:
  - command row
  - editor header/status strip
  - editor surface
  - metric cards
  - analyzer legend area
- Analyzer toggles now read as real stateful controls instead of plain buttons.
- The overlay/provider relationship is surfaced directly in the editor shell.

## Implementation Details

- Rebuilt the expanded editor area in `MainWindow.xaml` into a more deliberate multi-section layout.
- Added a reusable command-toggle style for the analyzer controls.
- Added new view-model projection types for:
  - editor status metrics
  - analyzer legend metrics
- Exposed new summary properties from `MainWindowViewModel` to drive the richer shell copy:
  - editor readiness
  - analyzer toggle state
  - overlay/provider support
- Kept the existing editor input flow and floating suggestion overlay wiring intact so this sprint remained a UI-structure lift rather than a behavior rewrite.

## Files Changed

- `src/WordSuggestorWindows.App/MainWindow.xaml` - rebuilt the expanded editor shell layout
- `src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` - added status/legend projection data and shell summary properties
- `src/WordSuggestorWindows.App/ViewModels/AnalyzerLegendMetric.cs` - added analyzer legend projection type
- `src/WordSuggestorWindows.App/ViewModels/EditorStatusMetric.cs` - added editor status metric projection type
- `docs/Architecture.md` - updated architecture baseline status
- `docs/Plan.md` - marked `WSA-UX-002` done
- `docs/ParityMatrix.md` - recorded editor structure parity baseline
- `docs/ManualSmoke.md` - updated smoke checklist for the rebuilt editor surface
- `docs/UiParityPlan.md` - recorded structure/layout implementation status
- `docs/changes/2026-04-10_wsa_ux_002_windows_internal_editor_surface_parity.md` - this note

## Feature Flags / Settings

- No persisted settings added.
- Analyzer controls still operate as runtime shell state in the Windows baseline.

## Logging / Telemetry

- No telemetry added.
- Existing in-app status messaging remains the primary operator-facing signal.

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild`
  - `Get-Process WordSuggestorWindows.App | Select-Object ProcessName, Id, Responding`
- Result:
  - WPF app build: `PASS`
  - Updated editor shell launch: `PASS`
  - Process check after launch: `WordSuggestorWindows.App` remained responsive

## Known Limitations / Follow-up

- This sprint improves structure and shell presentation, not full attributed-text styling parity.
- Word-class coloring, underline rendering, and right-click correction behavior remain future work.
- External-app capture/caret parity is still deferred to `WSA-RT-003`.

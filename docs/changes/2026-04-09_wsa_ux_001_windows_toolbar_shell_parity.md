# Windows Toolbar Shell Parity

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `done`
- Sprint ID: `WSA-UX-001_windows_toolbar_shell_parity`

## Summary

Replaced the temporary standard WPF window with the first Windows-native floating toolbar shell, aligned to the macOS control ordering and startup model while keeping the existing local `WordSuggestorCore` bridge functional.

## User-Visible Behavior

- The Windows app no longer starts as a generic large document window.
- It now starts as a compact floating toolbar shell with the WordSuggestor app icon, global toggle, language selector, feature buttons, and right-side expand/collapse control.
- Expanding the shell opens the internal editor downward beneath the toolbar instead of switching to a separate layout.
- The expanded shell includes:
  - command row
  - editor surface
  - status row
  - temporary live suggestion preview strip
  - analyzer legend/footer structure
- In the internal editor, `Ctrl+1` through `Ctrl+0` now picks the corresponding local suggestion when present.

## Implementation Details

- Copied the shared WordSuggestor app icon from the macOS asset catalog into the Windows app project and registered it as a WPF resource.
- Rebuilt `MainWindow.xaml` into a borderless, transparent-shell WPF window with:
  - floating toolbar chrome,
  - Windows-friendly button styling,
  - custom switch styling for the global toggle,
  - expandable editor host area.
- Added top-center initial positioning and expand/collapse shell resizing in `MainWindow.xaml.cs`, preserving the toolbar top edge while the window grows downward.
- Added drag-to-move behavior on non-interactive toolbar surface regions.
- Reworked `MainWindowViewModel` so it now owns shell state in addition to suggestion state:
  - editor expanded/collapsed state,
  - global toggle state,
  - language selection baseline,
  - temporary analyzer toggle states,
  - editor metrics,
  - suggestion preview captioning.
- Kept the current CLI-backed suggestion flow intact and surfaced it in a temporary in-app preview strip until the dedicated overlay panel sprint.

## Files Changed

- `src/WordSuggestorWindows.App/Assets/AppIcon.png` - shared WordSuggestor app icon copied into the Windows project
- `src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj` - registered app icon asset as a WPF resource
- `src/WordSuggestorWindows.App/MainWindow.xaml` - replaced normal window layout with floating toolbar shell and expanded editor shell UI
- `src/WordSuggestorWindows.App/MainWindow.xaml.cs` - added shell sizing, top-center positioning, drag behavior, toolbar action wiring, and `Ctrl+digit` suggestion handling
- `src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` - added shell state, editor metrics, toolbar action messaging, and suggestion preview support
- `docs/Plan.md` - marked `WSA-UX-001` done and moved the next step to `WSA-UX-002`
- `docs/ParityMatrix.md` - updated shell-parity status
- `docs/ManualSmoke.md` - updated smoke checklist for the floating toolbar shell model
- `docs/changes/2026-04-09_wsa_ux_001_windows_toolbar_shell_parity.md` - this note

## Feature Flags / Settings

- No persisted feature flags added in this sprint.
- The following runtime shell states are now modeled in the Windows app baseline:
  - editor expanded/collapsed
  - global suggestions enabled/disabled
  - language selection baseline

## Logging / Telemetry

- No telemetry added.
- Toolbar and editor placeholder actions currently report status through the existing in-app status messaging.

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild`
- Result:
  - WPF app build: `PASS`
  - Updated shell launch: `PASS`
  - Process check after launch: `WordSuggestorWindows.App` remained responsive

## Known Limitations / Follow-up

- The dedicated floating suggestion overlay is not implemented yet; the expanded shell uses a temporary in-app suggestion preview strip.
- The command row, status row, and analyzer legend currently establish layout parity, not full analysis parity.
- `WSA-UX-002` is the next sprint needed to bring the expanded editor surface closer to the macOS editor behavior and styling.

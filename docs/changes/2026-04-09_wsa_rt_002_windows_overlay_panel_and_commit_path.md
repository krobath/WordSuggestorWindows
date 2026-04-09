# Windows Overlay Panel And Commit Path

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `done`
- Sprint ID: `WSA-RT-002_windows_overlay_panel_and_commit_path`

## Summary

Replaced the temporary in-editor suggestion preview with a separate floating Windows overlay that follows the editor caret when possible, falls back to a stable static anchor when needed, and preserves the macOS candidate-panel semantics around paging and quick selection.

## User-Visible Behavior

- Suggestions are no longer rendered inside the expanded editor shell.
- The Windows app now shows a separate floating suggestion overlay window.
- The overlay can switch between:
  - static placement
  - follow-caret placement
- When follow-caret cannot resolve a trustworthy caret anchor, the overlay falls back to a static anchor near the editor surface.
- Suggestions are paged with up to 10 entries per page and up to 4 pages total.
- Users can select visible candidates with:
  - `Tab`
  - `Ctrl+1` through `Ctrl+0`
  - clicking a suggestion row
- Users can change overlay pages with `Ctrl+Left` and `Ctrl+Right`.

## Implementation Details

- Added `SuggestionOverlayWindow` as a dedicated borderless topmost WPF surface for suggestion rendering.
- Added placement-mode state to `MainWindowViewModel` so the overlay can move between static mode and follow-caret mode without leaking view logic into the provider bridge.
- Added overlay pagination state and view models:
  - 10 visible suggestions per page
  - 4 page cap
  - 40 candidate fetch ceiling from the CLI bridge
- Updated the CLI-backed suggestion provider to request enough candidates for the overlay paging model.
- Added caret-based overlay anchoring from the internal editor using `TextBox.GetRectFromCharacterIndex`.
- Added static fallback anchoring when the caret rectangle cannot be resolved.
- Added keyboard handling for:
  - `Ctrl+1` through `Ctrl+0`
  - `Ctrl+Left`
  - `Ctrl+Right`
- Removed the temporary in-shell suggestion preview strip from `MainWindow.xaml`.

## Files Changed

- `src/WordSuggestorWindows.App/MainWindow.xaml` - removed preview strip and wired editor events needed by the overlay
- `src/WordSuggestorWindows.App/MainWindow.xaml.cs` - added overlay lifetime management, caret/static positioning logic, and paging shortcuts
- `src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml` - added the dedicated floating suggestion surface
- `src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs` - added overlay interaction wiring for placement mode, paging, and click-to-accept
- `src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs` - increased candidate fetch ceiling to support paging parity
- `src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` - added overlay state, pagination, placement mode, and visible-page projection logic
- `src/WordSuggestorWindows.App/ViewModels/SuggestionOverlayEntry.cs` - added a UI projection model for visible overlay rows
- `src/WordSuggestorWindows.App/ViewModels/SuggestionPlacementMode.cs` - added explicit static/follow-caret placement mode enum
- `docs/Architecture.md` - updated overlay phase status
- `docs/Plan.md` - marked `WSA-RT-002` done and refreshed next-step ordering
- `docs/ParityMatrix.md` - updated overlay, pagination, fallback, and shortcut parity status
- `docs/ManualSmoke.md` - updated smoke checklist for the floating overlay path
- `docs/UiParityPlan.md` - marked overlay parity as implemented for the internal editor baseline
- `docs/changes/2026-04-09_wsa_rt_002_windows_overlay_panel_and_commit_path.md` - this note

## Feature Flags / Settings

- No persisted settings were added in this sprint.
- Placement mode currently lives as runtime state in the Windows app view model.

## Logging / Telemetry

- No telemetry added.
- Overlay status continues to surface through the existing in-app status messaging.

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild`
  - `Get-Process WordSuggestorWindows.App | Select-Object ProcessName, Id, MainWindowTitle, Responding`
- Result:
  - WPF app build: `PASS`
  - Overlay-enabled app launch: `PASS`
  - Process check after launch: `WordSuggestorWindows.App` remained responsive

## Known Limitations / Follow-up

- Follow-caret currently targets only the internal Windows editor and does not yet track third-party applications.
- The current anchor logic uses the editor caret rectangle and a static editor-relative fallback; external caret APIs will be introduced later in `WSA-RT-003`.
- The overlay now matches the product interaction model, but visual refinement against the macOS screenshots is still pending.
- Right-click correction/context popover parity remains future work in `WSA-RT-004`.

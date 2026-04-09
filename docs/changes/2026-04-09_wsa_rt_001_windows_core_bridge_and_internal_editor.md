# Windows Core Bridge And Internal Editor Baseline

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `partial`
- Sprint ID: `WSA-RT-001_windows_core_bridge_and_internal_editor`

## Summary

Implemented the first runnable Windows app baseline: a WPF app shell with an internal editor, local suggestion list, and a bridge abstraction that calls `WordSuggestorCore` through the existing Swift CLI surface.

## User-Visible Behavior

- A native Windows app window now exists in `WordSuggestorWindows`.
- Users can type into an internal editor surface and see a suggestion list update from the Windows-side suggestion provider flow.
- Users can accept a selected suggestion back into the internal editor with button, double-click, or `Tab`.

## Implementation Details

- Scaffolded a `.NET 9` WPF solution and app project.
- Added `MainWindowViewModel` for editor text, debounce, suggestion state, and accept behavior.
- Added `ISuggestionProvider` so the UI is not coupled directly to process-launch logic.
- Added `WordSuggestorCoreCliSuggestionProvider`, which:
  - locates the workspace root,
  - resolves the `WordSuggestorCore` repo and Danish SQLite pack,
  - prefers a prebuilt `WordSuggestorSuggestCLI` binary,
  - falls back to `swift run` when no prebuilt CLI is found.
- Added local scripts to make validation reproducible on this Windows workspace:
  - `scripts/build_app.ps1`
  - `scripts/test_core_cli.ps1`
- Added local `NuGet.Config` to keep package restore/build state workspace-local.

## Files Changed

- `WordSuggestorWindows.sln` - created Windows solution
- `NuGet.Config` - local NuGet configuration baseline
- `.gitignore` - ignores local build/cache directories
- `README.md` - documents new build and diagnostic scripts
- `src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj` - WPF app project scaffold
- `src/WordSuggestorWindows.App/MainWindow.xaml` - internal editor and suggestions UI
- `src/WordSuggestorWindows.App/MainWindow.xaml.cs` - code-behind for selection/caret/accept wiring
- `src/WordSuggestorWindows.App/Models/SuggestionItem.cs` - suggestion DTO for UI
- `src/WordSuggestorWindows.App/Services/ISuggestionProvider.cs` - bridge contract
- `src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs` - Swift CLI bridge implementation
- `src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` - app state and interaction logic
- `src/WordSuggestorWindows.App/ViewModels/RelayCommand.cs` - command helper for UI actions
- `scripts/build_app.ps1` - reproducible local app build command
- `scripts/test_core_cli.ps1` - reproducible local core bridge diagnostic
- `docs/Plan.md` - sprint status updated
- `docs/changes/2026-04-09_wsa_rt_001_windows_core_bridge_and_internal_editor.md` - this note

## Feature Flags / Settings

- No user-facing feature flags added in this sprint.
- Optional environment variable:
  - `WORDSUGGESTOR_SUGGEST_CLI_PATH` can point the Windows app to a prebuilt `WordSuggestorSuggestCLI` binary.

## Logging / Telemetry

- No telemetry added yet.
- Runtime bridge failures currently surface as UI status messages in the app window.

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1`
- Result:
  - WPF app build: `PASS`
  - Core CLI diagnostic: `FAIL`
  - Current failure is environmental, not a Windows app compile failure:
    - Swift build stops because `WordSuggestorCore\Sources\CSQLite\shim.h` cannot resolve `sqlite3.h` on this machine.

## Known Limitations / Follow-up

- Follow-up on `2026-04-09`: local CLI bootstrap was added in `WSA-DX-002`, and `scripts/test_core_cli.ps1` now passes against the built Windows CLI.
- Remaining next step is manual Windows UI smoke for the WPF app using the live bridge path.
- External-app capture, caret tracking, and overlay placement are not part of this sprint yet.

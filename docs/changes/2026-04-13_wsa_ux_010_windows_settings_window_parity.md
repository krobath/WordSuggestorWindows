# WSA-UX-010_windows_settings_window_parity

## Summary

Added the first Windows-native settings window and local settings persistence baseline.

## User-visible behavior

- Clicking the toolbar settings gear opens a native Windows settings window.
- The settings categories match the current macOS settings model:
  - `Generelt`
  - `Ordforslag`
  - `Tekstanalyse`
  - `Fejlsporing`
  - `Avanceret`
- Supported settings are saved locally and applied to the running Windows session.
- Placeholder-only settings are visible but disabled where Windows does not yet have runtime support.

## Implementation details

- Added `AppSettingsSnapshot` as the Windows settings model.
- Added `WindowsAppSettingsService` for local JSON persistence at `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`.
- Added `SettingsWindow` as a WPF settings surface.
- Wired the toolbar settings action in `MainWindow` to open/activate the settings window.
- Wired active settings into `MainWindowViewModel`:
  - language,
  - suggestion placement mode,
  - global suggestions toggle,
  - analyzer coloring,
  - semantic diagnostics,
  - punctuation diagnostics,
  - error tracking enabled/disabled,
  - system speech preference persistence.
- Preserved placeholder parity for domain lists, sentence-example storage, debug logging, performance instrumentation, and deeper voice/runtime options.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/App.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WindowsAppSettingsService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/changes/2026-04-13_wsa_ux_010_windows_settings_window_parity.md`

## Feature flags / settings impact

Added local Windows settings persistence under the current user profile. No shared cross-platform profile sync was added.

## Logging / telemetry impact

No telemetry was added. Settings stay local to the Windows user profile.

## Validation performed

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process was stopped after smoke)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorWindows diff --check` -> `PASS` (only Git CRLF/LF normalization warning)
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

## Known limitations / follow-up

- The settings baseline is Windows-local JSON, not a shared cross-platform settings profile.
- Disabled controls intentionally preserve macOS semantics without implying runtime support.
- Domain-list management, full debug/performance toggles, detailed voice/runtime configuration, and profile sync remain follow-up work.
- Manual GUI smoke is still needed to confirm the settings window opens from the gear and persists a changed setting interactively. The automated launch smoke only verifies that the app starts without a WPF/XAML crash.

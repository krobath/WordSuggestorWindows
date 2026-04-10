# WSA-RT-005_windows_overlay_static_drag_and_rich_rows

Date: `2026-04-10`
Status: `Done`

## Summary

Upgraded the Windows suggestion overlay so static placement now behaves as a true user-chosen resting position, and each suggestion row now carries richer macOS-style metadata and actions.

## User-visible behavior

- Static placement means the user can drag the overlay header to a chosen screen location, and the overlay stays there until moved again or the app is restarted.
- Suggestion rows now show the term plus an inline match label in parentheses.
- Rows now show a second metadata line with ordklasse and grammar tag when the engine provides that data.
- Row backgrounds now distinguish ordinary, phonetic, misspelling, and synonym suggestions.
- Each row now exposes a speaker button and an info button.
- The speaker button reads the suggestion aloud through a Windows-native SAPI path.
- The info button opens a lightweight popup with match-kind, POS, grammar, candidate type, and score details.

## Implementation details

- Extended `WordSuggestorSuggestCLI` to emit `type`, `pos`, and `gram` in its JSON output so the Windows app can render richer rows without changing the macOS app code.
- Added a Windows-side presentation helper that maps engine kinds and POS values into Danish UI labels and row tint colors.
- Rebuilt the WPF overlay row template around metadata, tinting, and per-row actions.
- Added manual static drag handling in the overlay window and stored the resulting top-left point in the main window runtime state.
- Kept follow-caret positioning unchanged except when runtime confidence is missing, where the overlay still falls back to static behavior.
- Used a PowerShell/SAPI bridge for row-level TTS so the feature works in the current `net9.0-windows` WPF setup without adding a fragile desktop-only assembly dependency.

## Files changed

- `WordSuggestorCore/Tools/SuggestCLI/main.swift`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/SuggestionItem.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/SuggestionPresentation.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/OverlaySpeechService.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/SuggestionOverlayEntry.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs`
- `WordSuggestorWindows/docs/Architecture.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS`
- Process check after launch showed `WordSuggestorWindows.App` remained responsive.

## Follow-up

- Persist manual static placement across app restarts if the product wants static placement to survive relaunch.
- Replace the lightweight row info popup with the fuller future word-insight surface when `WSA-RT-004` lands.
- Carry the same richer row semantics forward into external-app overlay work in `WSA-RT-003`.

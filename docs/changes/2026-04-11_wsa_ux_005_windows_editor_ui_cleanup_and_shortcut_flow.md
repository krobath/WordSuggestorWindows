# WSA-UX-005_windows_editor_ui_cleanup_and_shortcut_flow

Date: `2026-04-11`
Status: `Done`

## Summary

Cleaned up duplicated editor information cards, restored visible Danish UI labels, and tightened the keyboard flow for accepting suggestions and paging through overlay pages.

## User-visible behavior

- The internal editor no longer shows the informational card above the text input that repeated analyzer/toggle state.
- The editor no longer shows the informational card directly below the text input that repeated overlay placement/page/provider state.
- The lower analyzer panel no longer shows extra placement/provider cards.
- Static/follow-caret state is now represented visually on the overlay placement buttons.
- Accepting a suggestion with `Tab` or `Ctrl+1` through `Ctrl+0` inserts the selected word followed by a space, with the caret after the space.
- `Ctrl+Left` and `Ctrl+Right` page the overlay more reliably because the shortcut is handled by both the main window and the overlay window.
- Danish labels in the visible UI now use æ, ø, and å instead of ASCII fallbacks or mojibake.

## Implementation details

- Removed the editor header/status card and overlay support card from `MainWindow.xaml`.
- Removed duplicate overlay placement/provider cards from the analyzer panel.
- Converted overlay placement controls from plain buttons to checked toggle buttons bound to `IsStaticPlacementMode` and `IsFollowCaretPlacementMode`.
- Added `MainWindow_OnPreviewKeyDown` and `SuggestionOverlayWindow_OnPreviewKeyDown` shortcut paths.
- Updated token replacement to always add a trailing space unless the existing suffix already begins with whitespace.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/Plan.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap -SampleText "Jeg prøver at læ"` -> `PASS`
- Recent Application event log check after launch showed no new `WordSuggestorWindows.App` crash event.

## Follow-up

- Continue with the visual parity pass against the macOS screenshots, especially suggestion overlay proportions and chrome.
